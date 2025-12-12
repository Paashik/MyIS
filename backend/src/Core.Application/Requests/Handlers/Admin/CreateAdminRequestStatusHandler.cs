using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class CreateAdminRequestStatusHandler
{
    private readonly IRequestStatusRepository _requestStatusRepository;

    public CreateAdminRequestStatusHandler(IRequestStatusRepository requestStatusRepository)
    {
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
    }

    public async Task<RequestStatusDto> Handle(CreateAdminRequestStatusCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));

        var code = new RequestStatusCode(command.Code);
        var name = (command.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name is required.");

        if (await _requestStatusRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new InvalidOperationException($"RequestStatus with code '{code.Value}' already exists.");
        }

        var status = new RequestStatus(
            RequestStatusId.New(),
            code,
            name,
            command.IsFinal,
            command.Description,
            command.IsActive);

        await _requestStatusRepository.AddAsync(status, cancellationToken);

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

