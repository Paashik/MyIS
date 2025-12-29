using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Commands.Workflow;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Handlers.Workflow;
using MyIS.Core.Application.Requests.Queries;

namespace MyIS.Core.WebApi.Controllers.Requests;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RequestsController : ControllerBase
{
    private readonly CreateRequestHandler _createHandler;
    private readonly UpdateRequestHandler _updateHandler;
    private readonly DeleteRequestHandler _deleteHandler;
    private readonly SearchRequestsHandler _searchHandler;
    private readonly GetRequestByIdHandler _getByIdHandler;
    private readonly GetRequestHistoryHandler _historyHandler;
    private readonly GetRequestCommentsHandler _commentsHandler;
    private readonly AddRequestCommentHandler _addCommentHandler;
    private readonly GetRequestActionsHandler _actionsHandler;
    private readonly SubmitRequestHandler _submitHandler;
    private readonly StartReviewRequestHandler _startReviewHandler;
    private readonly ApproveRequestHandler _approveHandler;
    private readonly RejectRequestHandler _rejectHandler;
    private readonly StartWorkOnRequestHandler _startWorkHandler;
    private readonly CompleteRequestHandler _completeHandler;
    private readonly CloseRequestHandler _closeHandler;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        CreateRequestHandler createHandler,
        UpdateRequestHandler updateHandler,
        DeleteRequestHandler deleteHandler,
        SearchRequestsHandler searchHandler,
        GetRequestByIdHandler getByIdHandler,
        GetRequestHistoryHandler historyHandler,
        GetRequestCommentsHandler commentsHandler,
        AddRequestCommentHandler addCommentHandler,
        GetRequestActionsHandler actionsHandler,
        SubmitRequestHandler submitHandler,
        StartReviewRequestHandler startReviewHandler,
        ApproveRequestHandler approveHandler,
        RejectRequestHandler rejectHandler,
        StartWorkOnRequestHandler startWorkHandler,
        CompleteRequestHandler completeHandler,
        CloseRequestHandler closeHandler,
        ILogger<RequestsController> logger)
    {
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _deleteHandler = deleteHandler ?? throw new ArgumentNullException(nameof(deleteHandler));
        _searchHandler = searchHandler ?? throw new ArgumentNullException(nameof(searchHandler));
        _getByIdHandler = getByIdHandler ?? throw new ArgumentNullException(nameof(getByIdHandler));
        _historyHandler = historyHandler ?? throw new ArgumentNullException(nameof(historyHandler));
        _commentsHandler = commentsHandler ?? throw new ArgumentNullException(nameof(commentsHandler));
        _addCommentHandler = addCommentHandler ?? throw new ArgumentNullException(nameof(addCommentHandler));
        _actionsHandler = actionsHandler ?? throw new ArgumentNullException(nameof(actionsHandler));
        _submitHandler = submitHandler ?? throw new ArgumentNullException(nameof(submitHandler));
        _startReviewHandler = startReviewHandler ?? throw new ArgumentNullException(nameof(startReviewHandler));
        _approveHandler = approveHandler ?? throw new ArgumentNullException(nameof(approveHandler));
        _rejectHandler = rejectHandler ?? throw new ArgumentNullException(nameof(rejectHandler));
        _startWorkHandler = startWorkHandler ?? throw new ArgumentNullException(nameof(startWorkHandler));
        _completeHandler = completeHandler ?? throw new ArgumentNullException(nameof(completeHandler));
        _closeHandler = closeHandler ?? throw new ArgumentNullException(nameof(closeHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Список заявок с фильтрами и пагинацией.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RequestListItemDto>>> SearchRequests(
        [FromQuery] Guid? requestTypeId,
        [FromQuery] Guid? requestStatusId,
        [FromQuery] string? direction,
        [FromQuery] bool onlyMine = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new SearchRequestsQuery
        {
            RequestTypeId = requestTypeId,
            RequestStatusId = requestStatusId,
            Direction = direction,
            OnlyMine = onlyMine,
            CurrentUserId = currentUserId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _searchHandler.Handle(query, cancellationToken);
        return Ok(result.Page);
    }

    /// <summary>
    /// Получение заявки по Id.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new GetRequestByIdQuery
        {
            Id = id,
            CurrentUserId = currentUserId
        };

        try
        {
            var result = await _getByIdHandler.Handle(query, cancellationToken);
            return Ok(result.Request);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Создание новой заявки.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(
        [FromBody] Contracts.Requests.CreateRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var command = new CreateRequestCommand
        {
            ManagerId = currentUserId,
            RequestTypeId = request.RequestTypeId,
            Title = request.Title,
            Description = request.Description,
            Lines = request.Lines is null
                ? null
                : Array.ConvertAll(request.Lines, l => new MyIS.Core.Application.Requests.Dto.RequestLineInputDto
                {
                    LineNo = l.LineNo,
                    ItemId = l.ItemId,
                    ExternalItemCode = l.ExternalItemCode,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitOfMeasureId = l.UnitOfMeasureId,
                    NeedByDate = l.NeedByDate,
                    SupplierName = l.SupplierName,
                    SupplierContact = l.SupplierContact,
                    ExternalRowReferenceId = l.ExternalRowReferenceId
                }),
            DueDate = request.DueDate,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityName = request.RelatedEntityName,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            TargetEntityName = request.TargetEntityName,
            BasisType = request.BasisType,
            BasisRequestId = request.BasisRequestId,
            BasisCustomerOrderId = request.BasisCustomerOrderId,
            BasisDescription = request.BasisDescription
        };

        var created = await _createHandler.Handle(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    /// <summary>
    /// Обновление полей заявки (без смены статуса).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Update(
        Guid id,
        [FromBody] Contracts.Requests.UpdateRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var command = new UpdateRequestCommand
        {
            Id = id,
            CurrentUserId = currentUserId,
            RequestTypeId = request.RequestTypeId,
            Title = request.Title,
            Description = request.Description,
            Lines = request.Lines is null
                ? null
                : Array.ConvertAll(request.Lines, l => new MyIS.Core.Application.Requests.Dto.RequestLineInputDto
                {
                    LineNo = l.LineNo,
                    ItemId = l.ItemId,
                    ExternalItemCode = l.ExternalItemCode,
                    Description = l.Description,
                    Quantity = l.Quantity,
                    UnitOfMeasureId = l.UnitOfMeasureId,
                    NeedByDate = l.NeedByDate,
                    SupplierName = l.SupplierName,
                    SupplierContact = l.SupplierContact,
                    ExternalRowReferenceId = l.ExternalRowReferenceId
                }),
            DueDate = request.DueDate,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityName = request.RelatedEntityName,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            TargetEntityName = request.TargetEntityName,
            BasisType = request.BasisType,
            BasisRequestId = request.BasisRequestId,
            BasisCustomerOrderId = request.BasisCustomerOrderId,
            BasisDescription = request.BasisDescription
        };

        try
        {
            var updated = await _updateHandler.Handle(command, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Удаление заявки.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var command = new DeleteRequestCommand
        {
            Id = id,
            CurrentUserId = currentUserId
        };

        try
        {
            await _deleteHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Доступные действия workflow для заявки (type + status + права).
    /// </summary>
    [HttpGet("{id:guid}/actions")]
    public async Task<ActionResult<Contracts.Requests.RequestAvailableActionsResponse>> GetActions(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new GetRequestActionsQuery
        {
            RequestId = id,
            CurrentUserId = currentUserId
        };

        try
        {
            var actions = await _actionsHandler.Handle(query, cancellationToken);
            return Ok(new Contracts.Requests.RequestAvailableActionsResponse { Actions = actions });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/submit")]
    public Task<ActionResult<RequestDto>> Submit(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new SubmitRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _submitHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/start-review")]
    public Task<ActionResult<RequestDto>> StartReview(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new StartReviewRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _startReviewHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public Task<ActionResult<RequestDto>> Approve(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new ApproveRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _approveHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<ActionResult<RequestDto>> Reject(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new RejectRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _rejectHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/start-work")]
    public Task<ActionResult<RequestDto>> StartWork(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new StartWorkOnRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _startWorkHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/complete")]
    public Task<ActionResult<RequestDto>> Complete(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new CompleteRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _completeHandler.Handle, cancellationToken);

    [HttpPost("{id:guid}/close")]
    public Task<ActionResult<RequestDto>> Close(Guid id, [FromBody] Contracts.Requests.RequestActionRequest? request, CancellationToken cancellationToken = default)
        => ExecuteWorkflowAction(id, request?.Comment, (rid, uid, cmt) => new CloseRequestCommand { RequestId = rid, CurrentUserId = uid, Comment = cmt }, _closeHandler.Handle, cancellationToken);

    private async Task<ActionResult<RequestDto>> ExecuteWorkflowAction<TCommand>(
        Guid requestId,
        string? comment,
        Func<Guid, Guid, string?, TCommand> commandFactory,
        Func<TCommand, CancellationToken, Task<RequestDto>> handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var command = commandFactory(requestId, currentUserId, comment);

        try
        {
            var dto = await handler(command, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// История изменений заявки.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<RequestHistoryItemDto[]>> GetHistory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new GetRequestHistoryQuery
        {
            RequestId = id,
            CurrentUserId = currentUserId
        };

        try
        {
            var result = await _historyHandler.Handle(query, cancellationToken);
            return Ok(result.Items);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Комментарии по заявке.
    /// </summary>
    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<RequestCommentDto[]>> GetComments(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new GetRequestCommentsQuery
        {
            RequestId = id,
            CurrentUserId = currentUserId
        };

        try
        {
            var result = await _commentsHandler.Handle(query, cancellationToken);
            return Ok(result.Items);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Добавление комментария к заявке.
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<RequestCommentDto>> AddComment(
        Guid id,
        [FromBody] Contracts.Requests.AddRequestCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var createdAt = request.CreatedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

        var command = new AddRequestCommentCommand
        {
            RequestId = id,
            AuthorId = currentUserId,
            Text = request.Text,
            CreatedAt = createdAt
        };

        try
        {
            var dto = await _addCommentHandler.Handle(command, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogWarning("Current user has no NameIdentifier claim.");
            return false;
        }

        if (!Guid.TryParse(raw, out userId))
        {
            _logger.LogWarning("Failed to parse NameIdentifier claim '{Claim}' as Guid.", raw);
            userId = Guid.Empty;
            return false;
        }

        return true;
    }
}



