using System;

namespace MyIS.Core.Application.Requests.Queries;

public sealed class GetRequestActionsQuery
{
    public Guid RequestId { get; init; }

    public Guid CurrentUserId { get; init; }
}

