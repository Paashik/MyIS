using System;

namespace MyIS.Core.Application.Requests.Queries.Admin;

public sealed class GetAdminRequestTypesQuery
{
    public Guid CurrentUserId { get; init; }
}

