using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class ReplaceAdminUserRolesCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid UserId { get; init; }
    public IReadOnlyList<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}

