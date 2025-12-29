using System;
using System.Collections.Generic;

namespace MyIS.Core.Domain.Organization;

public class OrgUnit
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public Guid? ParentId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<OrgUnitContact> Contacts { get; private set; } = new List<OrgUnitContact>();

    private OrgUnit()
    {
        // For EF Core
    }

    public OrgUnit(
        string name,
        string? code,
        Guid? parentId,
        Guid? managerEmployeeId,
        string? phone,
        string? email,
        bool isActive,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = NormalizeOptional(code);
        ParentId = parentId;
        ManagerEmployeeId = managerEmployeeId;
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
        IsActive = isActive;
        SortOrder = sortOrder;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(
        string name,
        string? code,
        Guid? parentId,
        Guid? managerEmployeeId,
        string? phone,
        string? email,
        bool isActive,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = name.Trim();
        Code = NormalizeOptional(code);
        ParentId = parentId;
        ManagerEmployeeId = managerEmployeeId;
        Phone = NormalizeOptional(phone);
        Email = NormalizeOptional(email);
        IsActive = isActive;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
