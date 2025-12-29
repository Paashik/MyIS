using System;

namespace MyIS.Core.Domain.Organization;

public class OrgUnitContact
{
    public Guid Id { get; private set; }
    public Guid OrgUnitId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public bool IncludeInRequest { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private OrgUnitContact()
    {
        // For EF Core
    }

    public OrgUnitContact(Guid orgUnitId, Guid employeeId, bool includeInRequest, int sortOrder)
    {
        if (orgUnitId == Guid.Empty)
        {
            throw new ArgumentException("OrgUnitId is required.", nameof(orgUnitId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId is required.", nameof(employeeId));
        }

        Id = Guid.NewGuid();
        OrgUnitId = orgUnitId;
        EmployeeId = employeeId;
        IncludeInRequest = includeInRequest;
        SortOrder = sortOrder;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(bool includeInRequest, int sortOrder)
    {
        IncludeInRequest = includeInRequest;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
