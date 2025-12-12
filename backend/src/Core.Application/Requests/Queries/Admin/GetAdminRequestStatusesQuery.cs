using System;

namespace MyIS.Core.Application.Requests.Queries.Admin;

public sealed class GetAdminRequestStatusesQuery
{
    public Guid CurrentUserId { get; init; }
}

