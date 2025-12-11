using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestHistory
{
    public Guid Id { get; private set; }

    public RequestId RequestId { get; private set; }

    public string Action { get; private set; } = null!;

    public Guid PerformedBy { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string OldValue { get; private set; } = null!;

    public string NewValue { get; private set; } = null!;

    public string? Comment { get; private set; }

    public Request? Request { get; private set; }

    private RequestHistory()
    {
        // For EF Core
    }

    private RequestHistory(
        Guid id,
        RequestId requestId,
        string action,
        Guid performedBy,
        DateTimeOffset timestamp,
        string oldValue,
        string newValue,
        string? comment)
    {
        if (requestId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestId cannot be empty.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (performedBy == Guid.Empty)
        {
            throw new ArgumentException("PerformedBy cannot be empty.", nameof(performedBy));
        }

        if (string.IsNullOrWhiteSpace(oldValue))
        {
            throw new ArgumentException("OldValue is required.", nameof(oldValue));
        }

        if (string.IsNullOrWhiteSpace(newValue))
        {
            throw new ArgumentException("NewValue is required.", nameof(newValue));
        }

        Id = id;
        RequestId = requestId;
        Action = action.Trim();
        PerformedBy = performedBy;
        Timestamp = timestamp;
        OldValue = oldValue;
        NewValue = newValue;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    public static RequestHistory CreateStatusChange(
        RequestId requestId,
        string action,
        Guid performedBy,
        DateTimeOffset timestamp,
        string oldValue,
        string newValue,
        string? comment)
    {
        return new RequestHistory(
            Guid.NewGuid(),
            requestId,
            action,
            performedBy,
            timestamp,
            oldValue,
            newValue,
            comment);
    }
}