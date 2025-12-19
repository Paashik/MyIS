using System;

namespace MyIS.Core.WebApi.Dto.Integration;

public class RunComponent2020SyncRequest
{
    public Guid ConnectionId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string? SyncMode { get; set; } // Delta | SnapshotUpsert | Overwrite
    public bool DryRun { get; set; }
}
