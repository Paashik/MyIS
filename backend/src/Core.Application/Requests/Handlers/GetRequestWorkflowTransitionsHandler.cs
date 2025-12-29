using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public sealed class GetRequestWorkflowTransitionsHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestTransitionRepository _transitionRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestWorkflowTransitionsHandler(
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _transitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<GetRequestWorkflowTransitionsResult> Handle(
        GetRequestWorkflowTransitionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        await _accessChecker.EnsureCanReadReferenceDataAsync(
            query.CurrentUserId,
            nameof(RequestTransition),
            cancellationToken);

        var statuses = await _requestStatusRepository.GetAllAsync(includeInactive: false, cancellationToken);
        var statusIdByCode = statuses.ToDictionary(s => s.Code.Value, s => s.Id.Value, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<MyIS.Core.Domain.Requests.Entities.RequestTransition> transitions;

        if (query.TypeId.HasValue)
        {
            if (query.TypeId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("TypeId is required.");
            }

            var typeId = new RequestTypeId(query.TypeId.Value);
            var type = await _requestTypeRepository.GetByIdAsync(typeId, cancellationToken);
            if (type is null)
            {
                throw new InvalidOperationException($"RequestType with id '{query.TypeId}' was not found.");
            }

            transitions = await _transitionRepository.GetAllByTypeAsync(typeId, includeDisabled: false, cancellationToken);
        }
        else
        {
            transitions = await _transitionRepository.GetAllAsync(includeDisabled: false, cancellationToken);
        }

        var result = new List<RequestWorkflowTransitionDto>(transitions.Count);

        foreach (var t in transitions)
        {
            if (!statusIdByCode.TryGetValue(t.FromStatusCode.Value, out var fromStatusId))
            {
                continue;
            }

            if (!statusIdByCode.TryGetValue(t.ToStatusCode.Value, out var toStatusId))
            {
                continue;
            }

            result.Add(new RequestWorkflowTransitionDto
            {
                Id = t.Id,
                RequestTypeId = t.RequestTypeId.Value,
                FromStatusId = fromStatusId,
                FromStatusCode = t.FromStatusCode.Value,
                ToStatusId = toStatusId,
                ToStatusCode = t.ToStatusCode.Value,
                ActionCode = t.ActionCode,
                RequiredPermission = t.RequiredPermission,
                IsEnabled = t.IsEnabled
            });
        }

        var ordered = result
            .OrderBy(x => x.RequestTypeId)
            .ThenBy(x => x.FromStatusCode)
            .ThenBy(x => x.ActionCode)
            .ThenBy(x => x.ToStatusCode)
            .ToList();

        return new GetRequestWorkflowTransitionsResult(ordered);
    }
}


