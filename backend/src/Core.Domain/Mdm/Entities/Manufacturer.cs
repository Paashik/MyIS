using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Manufacturer : IDeactivatable
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? FullName { get; private set; }

    public string? Site { get; private set; }

    public string? Note { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Manufacturer()
    {
        // For EF Core
    }

    public Manufacturer(string name, string? fullName, string? site, string? note)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        FullName = NormalizeOptional(fullName);
        Site = NormalizeOptional(site);
        Note = NormalizeOptional(note);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? fullName, string? site, string? note, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        FullName = NormalizeOptional(fullName);
        Site = NormalizeOptional(site);
        Note = NormalizeOptional(note);
        IsActive = isActive;
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
