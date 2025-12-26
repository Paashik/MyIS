using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Statuses.Entities;

public class Status : IDeactivatable
{
    public Guid Id { get; private set; }
    public Guid? GroupId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int? Color { get; private set; }
    public int? Flags { get; private set; }
    public int? SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Status()
    {
        Name = null!;
    }

    public Status(Guid? groupId, string name, string? description, int? color, int? flags, int? sortOrder)
    {
        if (groupId.HasValue && groupId.Value == Guid.Empty)
        {
            throw new ArgumentException("Status group id cannot be empty.", nameof(groupId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Status name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        GroupId = groupId;
        Name = name.Trim();
        Description = NormalizeOptional(description);
        Color = color;
        Flags = flags;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFromExternal(string name, string? description, int? color, int? flags, int? sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Status name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = NormalizeOptional(description);
        Color = color;
        Flags = flags;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeGroup(Guid? groupId)
    {
        if (groupId.HasValue && groupId.Value == Guid.Empty)
        {
            throw new ArgumentException("Status group id cannot be empty.", nameof(groupId));
        }

        if (GroupId == groupId)
        {
            return;
        }

        GroupId = groupId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
