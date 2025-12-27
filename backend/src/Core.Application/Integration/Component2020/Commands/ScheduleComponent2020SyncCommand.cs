using System;

namespace MyIS.Core.Application.Integration.Component2020.Commands;

public class ScheduleComponent2020SyncCommand
{
    public Component2020SyncScope Scope { get; set; }
    public bool DryRun { get; set; }
    public string CronExpression { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class ScheduleComponent2020SyncResponse
{
    public Guid ScheduleId { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., "Scheduled", "Updated"
}

public class Component2020SyncScheduleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string CronExpression { get; set; } = null!;
    public Component2020SyncScope Scope { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
}
