using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class CounterpartyExternalLink
{
    public Guid Id { get; private set; }

    public Guid CounterpartyId { get; private set; }

    public string ExternalSystem { get; private set; } = null!;

    public string ExternalEntity { get; private set; } = null!;

    public string ExternalId { get; private set; } = null!;

    public int? SourceType { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private CounterpartyExternalLink()
    {
    }

    public CounterpartyExternalLink(
        Guid counterpartyId,
        string externalSystem,
        string externalEntity,
        string externalId,
        int? sourceType,
        DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem cannot be empty.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalEntity))
        {
            throw new ArgumentException("ExternalEntity cannot be empty.", nameof(externalEntity));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId cannot be empty.", nameof(externalId));
        }

        Id = Guid.NewGuid();
        CounterpartyId = counterpartyId;
        ExternalSystem = externalSystem.Trim();
        ExternalEntity = externalEntity.Trim();
        ExternalId = externalId.Trim();
        SourceType = sourceType;
        SyncedAt = syncedAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Touch(DateTimeOffset syncedAt, int? sourceType)
    {
        SyncedAt = syncedAt;
        SourceType = sourceType;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

