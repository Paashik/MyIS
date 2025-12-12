using System;

namespace MyIS.Core.Application.Security.Queries.Admin;

public sealed class GetAdminUsersQuery
{
    public Guid CurrentUserId { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
}

