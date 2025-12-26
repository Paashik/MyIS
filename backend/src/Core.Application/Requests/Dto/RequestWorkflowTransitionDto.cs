using System;

namespace MyIS.Core.Application.Requests.Dto;

public sealed class RequestWorkflowTransitionDto
{
    public Guid Id { get; init; }

    public Guid RequestTypeId { get; init; }

    public Guid FromStatusId { get; init; }
    public string FromStatusCode { get; init; } = null!;

    public Guid ToStatusId { get; init; }
    public string ToStatusCode { get; init; } = null!;

    public string ActionCode { get; init; } = null!;

    public string? RequiredPermission { get; init; }

    public bool IsEnabled { get; init; }
}

