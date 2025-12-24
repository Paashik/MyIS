using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Mdm.Entities;

public class ItemGroup : IDeactivatable
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Abbreviation { get; private set; }

    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public ItemGroup? Parent { get; private set; }

    private ItemGroup()
    {
        // For EF Core
    }

    public ItemGroup(string name, Guid? parentId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        ParentId = parentId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, Guid? parentId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        ParentId = parentId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetAbbreviation(string? abbreviation)
    {
        abbreviation = abbreviation?.Trim();
        if (string.IsNullOrWhiteSpace(abbreviation))
        {
            if (Abbreviation == null)
            {
                return;
            }

            Abbreviation = null;
            UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        abbreviation = abbreviation.ToUpperInvariant();

        if (abbreviation.Length > 10)
        {
            throw new ArgumentException("Abbreviation cannot be longer than 10 characters.", nameof(abbreviation));
        }

        foreach (var c in abbreviation)
        {
            if (!(c is >= 'A' and <= 'Z') && !(c is >= '0' and <= '9'))
            {
                throw new ArgumentException("Abbreviation can contain only latin letters A-Z and digits 0-9.", nameof(abbreviation));
            }
        }

        if (string.Equals(Abbreviation, abbreviation, StringComparison.Ordinal))
        {
            return;
        }

        Abbreviation = abbreviation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
