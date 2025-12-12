using System;

namespace MyIS.Core.Application.Security.Queries.Admin;

public sealed class GetAdminRolesQuery
{
    public Guid CurrentUserId { get; init; }
}

