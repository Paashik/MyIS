using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class ReplaceAdminUserRolesHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public ReplaceAdminUserRolesHandler(IUserRepository userRepository, IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
    }

    public async Task Handle(ReplaceAdminUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.UserId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(command));

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) throw new InvalidOperationException($"User '{command.UserId}' was not found.");

        var uniqueIds = (command.RoleIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        // validate role ids exist
        var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
        var roleIdSet = allRoles.Select(r => r.Id).ToHashSet();
        foreach (var id in uniqueIds)
        {
            if (!roleIdSet.Contains(id))
            {
                throw new InvalidOperationException($"Role '{id}' was not found.");
            }
        }

        await _userRoleRepository.ReplaceUserRolesAsync(command.UserId, uniqueIds, DateTimeOffset.UtcNow, cancellationToken);
    }
}

