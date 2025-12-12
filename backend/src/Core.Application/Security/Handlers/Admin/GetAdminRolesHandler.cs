using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminRolesHandler
{
    private readonly IRoleRepository _roleRepository;

    public GetAdminRolesHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(GetAdminRolesQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var roles = await _roleRepository.GetAllAsync(cancellationToken);

        var dtos = new List<RoleDto>(roles.Count);
        foreach (var r in roles)
        {
            dtos.Add(Map(r));
        }
        return dtos;
    }

    private static RoleDto Map(Role r)
    {
        return new RoleDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name
        };
    }
}

