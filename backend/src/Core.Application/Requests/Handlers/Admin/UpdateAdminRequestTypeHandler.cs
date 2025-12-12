using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class UpdateAdminRequestTypeHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;

    public UpdateAdminRequestTypeHandler(IRequestTypeRepository requestTypeRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
    }

    public async Task<RequestTypeDto> Handle(UpdateAdminRequestTypeCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var typeId = new RequestTypeId(command.Id);
        var type = await _requestTypeRepository.GetByIdAsync(typeId, cancellationToken);
        if (type is null)
        {
            throw new InvalidOperationException($"RequestType with id '{command.Id}' was not found.");
        }

        type.Rename(command.Name);

        if (!Enum.TryParse<RequestDirection>(command.Direction, ignoreCase: true, out var direction))
        {
            throw new InvalidOperationException("Direction must be Incoming or Outgoing.");
        }

        type.ChangeDirection(direction);
        type.ChangeDescription(command.Description);
        if (command.IsActive) type.Activate(); else type.Deactivate();

        await _requestTypeRepository.UpdateAsync(type, cancellationToken);

        return new RequestTypeDto
        {
            Id = type.Id.Value,
            Code = type.Code,
            Name = type.Name,
            Direction = type.Direction.ToString(),
            Description = type.Description,
            IsActive = type.IsActive
        };
    }
}

