using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class ParameterSet
{
    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public int? P0Id { get; private set; }

    public int? P1Id { get; private set; }

    public int? P2Id { get; private set; }

    public int? P3Id { get; private set; }

    public int? P4Id { get; private set; }

    public int? P5Id { get; private set; }

    public bool IsActive { get; private set; }

    public string? ExternalSystem { get; private set; }

    public string? ExternalId { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private ParameterSet()
    {
        // For EF Core
    }

    public ParameterSet(string code, string name, int? p0Id, int? p1Id, int? p2Id, int? p3Id, int? p4Id, int? p5Id, string? externalSystem, string? externalId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        P0Id = p0Id;
        P1Id = p1Id;
        P2Id = p2Id;
        P3Id = p3Id;
        P4Id = p4Id;
        P5Id = p5Id;
        IsActive = true;
        ExternalSystem = externalSystem?.Trim();
        ExternalId = externalId?.Trim();
        SyncedAt = DateTimeOffset.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, int? p0Id, int? p1Id, int? p2Id, int? p3Id, int? p4Id, int? p5Id, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        P0Id = p0Id;
        P1Id = p1Id;
        P2Id = p2Id;
        P3Id = p3Id;
        P4Id = p4Id;
        P5Id = p5Id;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSynced()
    {
        SyncedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}