using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminUserByIdHandler
{
    private readonly IUserRepository _userRepository;

    public GetAdminUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<UserDetailsDto> Handle(GetAdminUserByIdQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));
        if (query.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(query));

        var user = await _userRepository.GetByIdAsync(query.Id, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{query.Id}' was not found.");
        }

        var roleCodes = user.UserRoles
            .Select(ur => ur.Role.Code)
            .OrderBy(x => x)
            .ToArray();

        return new UserDetailsDto
        {
            Id = user.Id,
            Login = user.Login,
            IsActive = user.IsActive,
            EmployeeId = user.EmployeeId,
            EmployeeFullName = user.Employee?.ShortName ?? user.Employee?.FullName,
            RoleCodes = roleCodes
        };
    }
}

