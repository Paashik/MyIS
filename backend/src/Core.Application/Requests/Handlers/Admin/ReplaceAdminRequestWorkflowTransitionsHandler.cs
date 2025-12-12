using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class ReplaceAdminRequestWorkflowTransitionsHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestTransitionRepository _transitionRepository;

    public ReplaceAdminRequestWorkflowTransitionsHandler(
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _transitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
    }

    public async Task Handle(ReplaceAdminRequestWorkflowTransitionsCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.TypeCode)) throw new ArgumentException("TypeCode is required.", nameof(command));

        var type = await _requestTypeRepository.GetByCodeAsync(command.TypeCode.Trim(), cancellationToken);
        if (type is null)
        {
            throw new InvalidOperationException($"RequestType with code '{command.TypeCode}' was not found.");
        }

        // Load statuses once for id -> code mapping.
        var statuses = await _requestStatusRepository.GetAllAsync(includeInactive: true, cancellationToken);
        var statusById = statuses.ToDictionary(s => s.Id.Value, s => s.Code, EqualityComparer<Guid>.Default);

        var newTransitions = new List<RequestTransition>(command.Transitions.Length);

        foreach (var input in command.Transitions)
        {
            if (input.FromStatusId == Guid.Empty) throw new InvalidOperationException("FromStatusId is required.");
            if (input.ToStatusId == Guid.Empty) throw new InvalidOperationException("ToStatusId is required.");
            if (input.FromStatusId == input.ToStatusId) throw new InvalidOperationException("fromStatusId must not equal toStatusId.");

            if (!statusById.TryGetValue(input.FromStatusId, out var fromCode))
            {
                throw new InvalidOperationException($"FromStatusId '{input.FromStatusId}' was not found.");
            }

            if (!statusById.TryGetValue(input.ToStatusId, out var toCode))
            {
                throw new InvalidOperationException($"ToStatusId '{input.ToStatusId}' was not found.");
            }

            var actionCode = (input.ActionCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(actionCode)) throw new InvalidOperationException("ActionCode is required.");

            newTransitions.Add(new RequestTransition(
                Guid.NewGuid(),
                type.Id,
                fromCode,
                toCode,
                actionCode,
                input.RequiredPermission,
                input.IsEnabled));
        }

        await _transitionRepository.ReplaceForTypeAsync(type.Id, newTransitions, cancellationToken);
    }
}

