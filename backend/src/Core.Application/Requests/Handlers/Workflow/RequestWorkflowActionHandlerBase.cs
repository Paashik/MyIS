using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Workflow;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Workflow;

public abstract class RequestWorkflowActionHandlerBase
{
    protected readonly IRequestRepository RequestRepository;
    protected readonly IRequestTypeRepository RequestTypeRepository;
    protected readonly IRequestStatusRepository RequestStatusRepository;
    protected readonly IRequestTransitionRepository TransitionRepository;
    protected readonly IRequestsAccessChecker AccessChecker;

    protected RequestWorkflowActionHandlerBase(
        IRequestRepository requestRepository,
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository,
        IRequestsAccessChecker accessChecker)
    {
        RequestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        RequestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        RequestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        TransitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
        AccessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    protected async Task<RequestDto> ExecuteAsync(
        Guid requestIdValue,
        Guid currentUserId,
        string actionCode,
        string? comment,
        CancellationToken cancellationToken)
    {
        if (requestIdValue == Guid.Empty) throw new ArgumentException("RequestId is required.", nameof(requestIdValue));
        if (currentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(currentUserId));
        if (string.IsNullOrWhiteSpace(actionCode)) throw new ArgumentException("ActionCode is required.", nameof(actionCode));

        var requestId = new RequestId(requestIdValue);
        var request = await RequestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{requestIdValue}' was not found.");
        }

        var requestType = await RequestTypeRepository.GetByIdAsync(request.RequestTypeId, cancellationToken);
        if (requestType is null)
        {
            throw new InvalidOperationException($"RequestType with id '{request.RequestTypeId.Value}' was not found.");
        }

        var currentStatus = await RequestStatusRepository.GetByIdAsync(request.RequestStatusId, cancellationToken);
        if (currentStatus is null)
        {
            throw new InvalidOperationException($"RequestStatus with id '{request.RequestStatusId.Value}' was not found.");
        }

        // Поиск перехода
        var transition = await TransitionRepository.FindByTypeFromStatusAndActionAsync(
            request.RequestTypeId,
            currentStatus.Code,
            actionCode,
            cancellationToken);

        if (transition is null)
        {
            throw new InvalidOperationException(
                $"Transition is not allowed. Type='{requestType.Code}', From='{currentStatus.Code.Value}', Action='{actionCode}'.");
        }

        // Права
        await AccessChecker.EnsureCanPerformActionAsync(currentUserId, request, actionCode, transition.RequiredPermission, cancellationToken);

        // Доменные условия для SupplyRequest при выходе из Draft
        if (string.Equals(currentStatus.Code.Value, RequestStatusCode.Draft.Value, StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionCode, RequestActionCodes.Submit, StringComparison.OrdinalIgnoreCase))
        {
            request.EnsureBodyIsValidForSubmit(requestType.Code);
        }

        // Целевой статус
        var targetStatus = await RequestStatusRepository.GetByCodeAsync(transition.ToStatusCode, cancellationToken);
        if (targetStatus is null)
        {
            throw new InvalidOperationException($"Target RequestStatus '{transition.ToStatusCode.Value}' is not configured.");
        }

        // Смена статуса + история
        request.ChangeStatus(
            currentStatus,
            targetStatus,
            performedBy: currentUserId,
            timestamp: DateTimeOffset.UtcNow,
            action: actionCode,
            comment: comment);

        await RequestRepository.UpdateAsync(request, cancellationToken);

        return new RequestDto
        {
            Id = request.Id.Value,
            Title = request.Title,
            Description = request.Description,
            BodyText = request.Description,
            RequestTypeId = requestType.Id.Value,
            RequestTypeCode = requestType.Code,
            RequestTypeName = requestType.Name,
            RequestStatusId = targetStatus.Id.Value,
            RequestStatusCode = targetStatus.Code.Value,
            RequestStatusName = targetStatus.Name,
            InitiatorId = request.InitiatorId,
            InitiatorFullName = null,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            ExternalReferenceId = request.ExternalReferenceId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            DueDate = request.DueDate,
            Lines = MapToLineDtos(request)
        };
    }

    private static RequestLineDto[] MapToLineDtos(Request request)
    {
        if (request.Lines.Count == 0) return Array.Empty<RequestLineDto>();

        var result = new RequestLineDto[request.Lines.Count];
        var i = 0;
        foreach (var line in request.Lines)
        {
            result[i++] = new RequestLineDto
            {
                Id = line.Id.Value,
                LineNo = line.LineNo,
                ItemId = line.ItemId,
                ExternalItemCode = line.ExternalItemCode,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitOfMeasureId = line.UnitOfMeasureId,
                NeedByDate = line.NeedByDate,
                SupplierName = line.SupplierName,
                SupplierContact = line.SupplierContact,
                ExternalRowReferenceId = line.ExternalRowReferenceId
            };
        }

        Array.Sort(result, (a, b) => a.LineNo.CompareTo(b.LineNo));
        return result;
    }
}

