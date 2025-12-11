using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestHistoryItemDto
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public string Action { get; init; } = null!;

    public Guid PerformedBy { get; init; }

    public string? PerformedByFullName { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public string OldValue { get; init; } = null!;

    public string NewValue { get; init; } = null!;

    public string? Comment { get; init; }
}