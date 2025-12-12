using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminUserRolesHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public GetAdminUserRolesHandler(IUserRepository userRepository, IUserRoleRepository userRoleRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
    }

    public async Task<UserRolesDto> Handle(GetAdminUserRolesQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));
        if (query.UserId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(query));

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null) throw new InvalidOperationException($"User '{query.UserId}' was not found.");

        var roleIds = await _userRoleRepository.GetRoleIdsByUserIdAsync(query.UserId, cancellationToken);

        return new UserRolesDto
        {
            UserId = query.UserId,
            RoleIds = roleIds
        };
    }
}

