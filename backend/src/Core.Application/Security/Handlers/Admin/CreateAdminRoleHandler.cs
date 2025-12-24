using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class CreateAdminRoleHandler
{
    private static readonly Regex CodeRegex = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IRoleRepository _roleRepository;

    public CreateAdminRoleHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<RoleDto> Handle(CreateAdminRoleCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));

        var code = (command.Code ?? string.Empty).Trim();
        var name = (command.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Code is required.");
        if (!CodeRegex.IsMatch(code)) throw new InvalidOperationException("Code must match A-Za-z0-9._- (no spaces).");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name is required.");

        if (await _roleRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new InvalidOperationException($"Role with code '{code}' already exists.");
        }

        var role = Role.Create(
            id: Guid.NewGuid(),
            code: code,
            name: name,
            createdAt: DateTimeOffset.UtcNow);

        await _roleRepository.AddAsync(role, cancellationToken);

        return new RoleDto { Id = role.Id, Code = role.Code, Name = role.Name };
    }
}

