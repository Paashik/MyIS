using System;

namespace MyIS.Core.Infrastructure.Data.Entities.Integration;

public class Component2020SyncError
{
    public Guid Id { get; private set; }

    public Guid SyncRunId { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public string? ExternalEntity { get; private set; }

    public string? ExternalKey { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public string? Details { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Component2020SyncRun? SyncRun { get; private set; }

    private Component2020SyncError()
    {
        // For EF Core
    }

    public Component2020SyncError(Guid syncRunId, string entityType, string? externalEntity, string? externalKey, string message, string? details)
    {
        Id = Guid.NewGuid();
        SyncRunId = syncRunId;
        EntityType = entityType.Trim();
        ExternalEntity = externalEntity?.Trim();
        ExternalKey = externalKey?.Trim();
        Message = message.Trim();
        Details = details?.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
