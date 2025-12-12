using System;

namespace MyIS.Core.Application.Requests.Queries.Admin;

public sealed class GetAdminRequestWorkflowTransitionsQuery
{
    public Guid CurrentUserId { get; init; }

    public string? TypeCode { get; init; }
}

