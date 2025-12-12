using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class ArchiveAdminRequestTypeHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestRepository _requestRepository;

    public ArchiveAdminRequestTypeHandler(
        IRequestTypeRepository requestTypeRepository,
        IRequestRepository requestRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
    }

    public async Task Handle(ArchiveAdminRequestTypeCommand command, CancellationToken cancellationToken)
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

        var isUsed = await _requestRepository.AnyWithTypeIdAsync(typeId, cancellationToken);
        if (isUsed)
        {
            throw new InvalidOperationException("Нельзя архивировать тип: он используется в заявках.");
        }

        type.Deactivate();
        await _requestTypeRepository.UpdateAsync(type, cancellationToken);
    }
}

