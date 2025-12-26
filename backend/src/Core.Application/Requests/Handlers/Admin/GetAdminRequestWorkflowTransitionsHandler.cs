using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries.Admin;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class GetAdminRequestWorkflowTransitionsHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestTransitionRepository _transitionRepository;

    public GetAdminRequestWorkflowTransitionsHandler(
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _transitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
    }

    public async Task<IReadOnlyList<RequestWorkflowTransitionDto>> Handle(
        GetAdminRequestWorkflowTransitionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var statuses = await _requestStatusRepository.GetAllAsync(includeInactive: true, cancellationToken);

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

            transitions = await _transitionRepository.GetAllByTypeAsync(typeId, includeDisabled: true, cancellationToken);
        }
        else
        {
            transitions = await _transitionRepository.GetAllAsync(includeDisabled: true, cancellationToken);
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

        return result
            .OrderBy(x => x.RequestTypeId)
            .ThenBy(x => x.FromStatusCode)
            .ThenBy(x => x.ActionCode)
            .ThenBy(x => x.ToStatusCode)
            .ToList();
    }
}

