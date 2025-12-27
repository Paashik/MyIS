using System;
using System.Collections.Generic;

namespace MyIS.Core.Infrastructure.Data.Entities.Integration;

public class Component2020SyncRun
{
    public Guid Id { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public Guid? StartedByUserId { get; private set; }

    public string Scope { get; private set; } = string.Empty;

    public string Mode { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public int ProcessedCount { get; private set; }

    public int ErrorCount { get; private set; }

    public string? CountersJson { get; private set; }

    public string? Summary { get; private set; }

    public ICollection<Component2020SyncError> Errors { get; private set; } = new List<Component2020SyncError>();

    private Component2020SyncRun()
    {
        // For EF Core
    }

    public Component2020SyncRun(string scope, string mode, Guid? startedByUserId)
    {
        Id = Guid.NewGuid();
        Scope = scope.Trim();
        Mode = mode.Trim();
        StartedByUserId = startedByUserId;
        StartedAt = DateTimeOffset.UtcNow;
        Status = "Running";
    }

    public void Complete(string status, int processedCount, int errorCount, string? countersJson, string? summary)
    {
        FinishedAt = DateTimeOffset.UtcNow;
        Status = status.Trim();
        ProcessedCount = processedCount;
        ErrorCount = errorCount;
        CountersJson = countersJson;
        Summary = summary;
    }

    public void AddError(Component2020SyncError error)
    {
        Errors.Add(error);
    }
}
