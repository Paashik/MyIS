using System;

namespace MyIS.Core.Application.Requests.Queries.Admin;

public sealed class GetAdminRequestWorkflowTransitionsQuery
{
    public Guid CurrentUserId { get; init; }

    public Guid? TypeId { get; init; }
}

