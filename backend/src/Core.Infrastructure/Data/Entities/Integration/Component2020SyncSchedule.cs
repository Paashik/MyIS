using System;

namespace MyIS.Core.Infrastructure.Data.Entities.Integration;

public class Component2020SyncSchedule
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string CronExpression { get; private set; } = string.Empty;

    public string Scope { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset? LastRunAt { get; private set; }

    public DateTimeOffset? NextRunAt { get; private set; }

    public Component2020SyncSchedule()
    {
        // For EF Core
    }

    public Component2020SyncSchedule(string name, string cronExpression, string scope)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        CronExpression = cronExpression.Trim();
        Scope = scope.Trim();
        IsActive = true;
        CalculateNextRun();
    }

    public void Update(string name, string cronExpression, string scope, bool isActive)
    {
        Name = name.Trim();
        CronExpression = cronExpression.Trim();
        Scope = scope.Trim();
        IsActive = isActive;
        if (IsActive)
        {
            CalculateNextRun();
        }
        else
        {
            NextRunAt = null;
        }
    }

    public void MarkRun(DateTimeOffset runAt)
    {
        LastRunAt = runAt;
        CalculateNextRun();
    }

    private void CalculateNextRun()
    {
        // Simplified: next run in 1 hour for demo
        NextRunAt = DateTimeOffset.UtcNow.AddHours(1);
    }
}
