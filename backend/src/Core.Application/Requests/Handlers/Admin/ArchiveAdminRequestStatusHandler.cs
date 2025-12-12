using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class ArchiveAdminRequestStatusHandler
{
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTransitionRepository _transitionRepository;

    public ArchiveAdminRequestStatusHandler(
        IRequestStatusRepository requestStatusRepository,
        IRequestRepository requestRepository,
        IRequestTransitionRepository transitionRepository)
    {
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _transitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
    }

    public async Task Handle(ArchiveAdminRequestStatusCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var statusId = new RequestStatusId(command.Id);
        var status = await _requestStatusRepository.GetByIdAsync(statusId, cancellationToken);
        if (status is null)
        {
            throw new InvalidOperationException($"RequestStatus with id '{command.Id}' was not found.");
        }

        var isUsedInRequests = await _requestRepository.AnyWithStatusIdAsync(statusId, cancellationToken);
        if (isUsedInRequests)
        {
            throw new InvalidOperationException("Нельзя архивировать статус: он используется в заявках.");
        }

        var isUsedInTransitions = await _transitionRepository.AnyUsesStatusCodeAsync(status.Code, cancellationToken);
        if (isUsedInTransitions)
        {
            throw new InvalidOperationException("Нельзя архивировать статус: он используется в workflow-переходах.");
        }

        status.Deactivate();
        await _requestStatusRepository.UpdateAsync(status, cancellationToken);
    }
}

