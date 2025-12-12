using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class CreateAdminRequestTypeHandler
{
    private static readonly Regex CodeRegex = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IRequestTypeRepository _requestTypeRepository;

    public CreateAdminRequestTypeHandler(IRequestTypeRepository requestTypeRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
    }

    public async Task<RequestTypeDto> Handle(CreateAdminRequestTypeCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));

        var code = (command.Code ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Code is required.");
        if (!CodeRegex.IsMatch(code)) throw new InvalidOperationException("Code must match A-Za-z0-9._- (no spaces).");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name is required.");

        if (await _requestTypeRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new InvalidOperationException($"RequestType with code '{code}' already exists.");
        }

        if (!Enum.TryParse<RequestDirection>(command.Direction, ignoreCase: true, out var direction))
        {
            throw new InvalidOperationException("Direction must be Incoming or Outgoing.");
        }

        var type = new RequestType(
            RequestTypeId.New(),
            code,
            name,
            direction,
            command.Description,
            command.IsActive);

        await _requestTypeRepository.AddAsync(type, cancellationToken);

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

