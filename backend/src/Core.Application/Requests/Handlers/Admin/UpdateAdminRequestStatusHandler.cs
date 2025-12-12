using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class UpdateAdminRequestStatusHandler
{
    private readonly IRequestStatusRepository _requestStatusRepository;

    public UpdateAdminRequestStatusHandler(IRequestStatusRepository requestStatusRepository)
    {
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
    }

    public async Task<RequestStatusDto> Handle(UpdateAdminRequestStatusCommand command, CancellationToken cancellationToken)
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

        status.Rename(command.Name);
        status.ChangeDescription(command.Description);
        if (command.IsFinal) status.MarkFinal(); else status.MarkNonFinal();
        if (command.IsActive) status.Activate(); else status.Deactivate();

        await _requestStatusRepository.UpdateAsync(status, cancellationToken);

        return new RequestStatusDto
        {
            Id = status.Id.Value,
            Code = status.Code.Value,
            Name = status.Name,
            IsFinal = status.IsFinal,
            Description = status.Description,
            IsActive = status.IsActive
        };
    }
}

