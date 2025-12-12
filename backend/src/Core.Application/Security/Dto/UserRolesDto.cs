using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Security.Dto;

public sealed class UserRolesDto
{
    public Guid UserId { get; init; }
    public IReadOnlyList<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}

