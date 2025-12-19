namespace MyIS.Core.WebApi.Dto.Integration;

public class ScheduleComponent2020SyncRequest
{
    public string Scope { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public string CronExpression { get; set; } = null!;
    public bool IsActive { get; set; }
}
