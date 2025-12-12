using System;

namespace MyIS.Core.Application.Security.Queries.Admin;

public sealed class GetAdminUserRolesQuery
{
    public Guid CurrentUserId { get; init; }
    public Guid UserId { get; init; }
}

