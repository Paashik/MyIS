using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminUsersHandler
{
    private readonly IUserRepository _userRepository;

    public GetAdminUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<IReadOnlyList<UserListItemDto>> Handle(GetAdminUsersQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var users = await _userRepository.SearchAsync(query.Search, query.IsActive, cancellationToken);

        var dtos = new List<UserListItemDto>(users.Count);
        foreach (var u in users)
        {
            dtos.Add(Map(u));
        }

        return dtos;
    }

    private static UserListItemDto Map(User u)
    {
        var roleCodes = u.UserRoles
            .Select(ur => ur.Role.Code)
            .OrderBy(x => x)
            .ToArray();

        return new UserListItemDto
        {
            Id = u.Id,
            Login = u.Login,
            IsActive = u.IsActive,
            EmployeeId = u.EmployeeId,
            EmployeeFullName = u.Employee?.FullName,
            RoleCodes = roleCodes
        };
    }
}

