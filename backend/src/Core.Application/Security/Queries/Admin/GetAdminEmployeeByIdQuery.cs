using System;

namespace MyIS.Core.Application.Security.Queries.Admin;

public sealed class GetAdminEmployeeByIdQuery
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
}

