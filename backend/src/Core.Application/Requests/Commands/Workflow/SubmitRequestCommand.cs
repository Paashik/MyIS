using System;

namespace MyIS.Core.Application.Requests.Commands.Workflow;

public sealed class SubmitRequestCommand
{
    public Guid RequestId { get; init; }

    public Guid CurrentUserId { get; init; }

    public string? Comment { get; init; }
}

