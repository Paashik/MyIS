using System;

namespace MyIS.Core.Application.Requests.Commands.Admin;

public sealed class ReplaceAdminRequestWorkflowTransitionsCommand
{
    public Guid CurrentUserId { get; init; }

    public string TypeCode { get; init; } = null!;

    public RequestWorkflowTransitionInputDto[] Transitions { get; init; } = [];
}

public sealed class RequestWorkflowTransitionInputDto
{
    public Guid FromStatusId { get; init; }
    public Guid ToStatusId { get; init; }

    public string ActionCode { get; init; } = null!;
    public string? RequiredPermission { get; init; }
    public bool IsEnabled { get; init; } = true;
}

