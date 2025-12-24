using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class UpdateAdminRoleHandler
{
    private readonly IRoleRepository _roleRepository;

    public UpdateAdminRoleHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<RoleDto> Handle(UpdateAdminRoleCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var role = await _roleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (role is null)
        {
            throw new InvalidOperationException($"Role '{command.Id}' was not found.");
        }

        var name = (command.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name is required.");

        role.Rename(name);
        await _roleRepository.UpdateAsync(role, cancellationToken);

        return new RoleDto { Id = role.Id, Code = role.Code, Name = role.Name };
    }
}

