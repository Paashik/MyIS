using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Queries;

namespace MyIS.Core.WebApi.Controllers.Requests;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RequestsController : ControllerBase
{
    private readonly CreateRequestHandler _createHandler;
    private readonly UpdateRequestHandler _updateHandler;
    private readonly SearchRequestsHandler _searchHandler;
    private readonly GetRequestByIdHandler _getByIdHandler;
    private readonly GetRequestHistoryHandler _historyHandler;
    private readonly GetRequestCommentsHandler _commentsHandler;
    private readonly AddRequestCommentHandler _addCommentHandler;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        CreateRequestHandler createHandler,
        UpdateRequestHandler updateHandler,
        SearchRequestsHandler searchHandler,
        GetRequestByIdHandler getByIdHandler,
        GetRequestHistoryHandler historyHandler,
        GetRequestCommentsHandler commentsHandler,
        AddRequestCommentHandler addCommentHandler,
        ILogger<RequestsController> logger)
    {
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _searchHandler = searchHandler ?? throw new ArgumentNullException(nameof(searchHandler));
        _getByIdHandler = getByIdHandler ?? throw new ArgumentNullException(nameof(getByIdHandler));
        _historyHandler = historyHandler ?? throw new ArgumentNullException(nameof(historyHandler));
        _commentsHandler = commentsHandler ?? throw new ArgumentNullException(nameof(commentsHandler));
        _addCommentHandler = addCommentHandler ?? throw new ArgumentNullException(nameof(addCommentHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Список заявок с фильтрами и пагинацией.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RequestListItemDto>>> SearchRequests(
        [FromQuery] Guid? requestTypeId,
        [FromQuery] Guid? requestStatusId,
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
            InitiatorId = currentUserId,
            RequestTypeId = request.RequestTypeId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            ExternalReferenceId = request.ExternalReferenceId
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
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            ExternalReferenceId = request.ExternalReferenceId
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