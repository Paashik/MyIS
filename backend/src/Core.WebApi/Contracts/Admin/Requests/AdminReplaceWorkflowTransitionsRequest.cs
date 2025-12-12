using System;

namespace MyIS.Core.WebApi.Contracts.Admin.Requests;

public sealed class AdminReplaceWorkflowTransitionsRequest
{
    public string TypeCode { get; init; } = null!;

    public AdminWorkflowTransitionItemRequest[] Transitions { get; init; } = [];
}

public sealed class AdminWorkflowTransitionItemRequest
{
    public Guid FromStatusId { get; init; }
    public Guid ToStatusId { get; init; }

    public string ActionCode { get; init; } = null!;
    public string? RequiredPermission { get; init; }
    public bool IsEnabled { get; init; } = true;
}

