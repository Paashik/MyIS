using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Currency
{
    public Guid Id { get; private set; }

    public string? Code { get; private set; }

    public string Name { get; private set; }

    public string? Symbol { get; private set; }

    public decimal? Rate { get; private set; }

    public bool IsActive { get; private set; }

    public string? ExternalSystem { get; private set; }

    public string? ExternalId { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Currency()
    {
        // For EF Core
    }

    public Currency(string? code, string name, string? symbol, decimal? rate, string? externalSystem, string? externalId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = NormalizeOptional(code);
        Name = name.Trim();
        Symbol = NormalizeOptional(symbol);
        Rate = rate;
        IsActive = true;
        ExternalSystem = NormalizeOptional(externalSystem);
        ExternalId = NormalizeOptional(externalId);
        SyncedAt = DateTimeOffset.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? symbol, decimal? rate, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        Symbol = NormalizeOptional(symbol);
        Rate = rate;
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

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
