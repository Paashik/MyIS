using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Supplier
{
    public Guid Id { get; private set; }

    public string? Code { get; private set; }

    public string Name { get; private set; }

    public string? FullName { get; private set; }

    public string? Inn { get; private set; }

    public string? Kpp { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? City { get; private set; }

    public string? Address { get; private set; }

    public string? Site { get; private set; }

    public string? Note { get; private set; }

    public int? ProviderType { get; private set; }

    public string? ExternalSystem { get; private set; }

    public string? ExternalId { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Supplier()
    {
        // For EF Core
    }

    public Supplier(string? code, string name, string? fullName, string? inn, string? kpp)
        : this(code, name, fullName, inn, kpp, null, null, null, null, null, null, null)
    {
    }

    public Supplier(
        string? code,
        string name,
        string? fullName,
        string? inn,
        string? kpp,
        string? email,
        string? phone,
        string? city,
        string? address,
        string? site,
        string? note,
        int? providerType)
    {
        Id = Guid.NewGuid();
        Code = NormalizeOptional(code);
        Name = name.Trim();
        FullName = fullName?.Trim();
        Inn = inn?.Trim();
        Kpp = kpp?.Trim();
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        City = NormalizeOptional(city);
        Address = NormalizeOptional(address);
        Site = NormalizeOptional(site);
        Note = NormalizeOptional(note);
        ProviderType = providerType;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? fullName, string? inn, string? kpp, bool isActive)
    {
        Name = name.Trim();
        FullName = fullName?.Trim();
        Inn = inn?.Trim();
        Kpp = kpp?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFromExternal(
        string name,
        string? fullName,
        string? inn,
        string? kpp,
        string? email,
        string? phone,
        string? city,
        string? address,
        string? site,
        string? note,
        int? providerType,
        bool isActive)
    {
        Update(name, fullName, inn, kpp, isActive);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        City = NormalizeOptional(city);
        Address = NormalizeOptional(address);
        Site = NormalizeOptional(site);
        Note = NormalizeOptional(note);
        ProviderType = providerType;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetExternalReference(string externalSystem, string externalId, DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem cannot be null or empty.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId cannot be null or empty.", nameof(externalId));
        }

        ExternalSystem = externalSystem.Trim();
        ExternalId = externalId.Trim();
        SyncedAt = syncedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
