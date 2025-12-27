using System;

namespace MyIS.Core.Infrastructure.Data.Entities.Integration;

public class Component2020SyncCursor
{
    public Guid ConnectionId { get; private set; }

    public string SourceEntity { get; private set; } = string.Empty;

    public string? LastProcessedKey { get; private set; }

    public DateTimeOffset LastSyncAt { get; private set; }

    public Component2020SyncCursor()
    {
        // For EF Core
    }

    public Component2020SyncCursor(Guid connectionId, string sourceEntity)
    {
        ConnectionId = connectionId;
        SourceEntity = sourceEntity.Trim();
        LastSyncAt = DateTimeOffset.UtcNow;
    }

    public void UpdateCursor(string? lastProcessedKey)
    {
        LastProcessedKey = lastProcessedKey?.Trim();
        LastSyncAt = DateTimeOffset.UtcNow;
    }
}
