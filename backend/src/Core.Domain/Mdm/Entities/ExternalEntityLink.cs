using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class ExternalEntityLink
{
    public Guid Id { get; private set; }

    public string EntityType { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    public string ExternalSystem { get; private set; } = null!;

    public string ExternalEntity { get; private set; } = null!;

    public string ExternalId { get; private set; } = null!;

    public int? SourceType { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private ExternalEntityLink()
    {
        // For EF Core
    }

    public ExternalEntityLink(
        string entityType,
        Guid entityId,
        string externalSystem,
        string externalEntity,
        string externalId,
        int? sourceType,
        DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("EntityType is required.", nameof(entityType));
        if (entityId == Guid.Empty) throw new ArgumentException("EntityId is required.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(externalSystem)) throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        if (string.IsNullOrWhiteSpace(externalEntity)) throw new ArgumentException("ExternalEntity is required.", nameof(externalEntity));
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("ExternalId is required.", nameof(externalId));

        Id = Guid.NewGuid();
        EntityType = entityType.Trim();
        EntityId = entityId;
        ExternalSystem = externalSystem.Trim();
        ExternalEntity = externalEntity.Trim();
        ExternalId = externalId.Trim();
        SourceType = sourceType;
        SyncedAt = syncedAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Touch(DateTimeOffset now, int? sourceType)
    {
        SourceType = sourceType;
        SyncedAt = now;
        UpdatedAt = now;
    }
}

