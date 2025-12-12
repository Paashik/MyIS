using System;
using System.Collections.Generic;

namespace MyIS.Core.WebApi.Contracts.Admin.Security;

public sealed class AdminReplaceUserRolesRequest
{
    public IReadOnlyList<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
}

