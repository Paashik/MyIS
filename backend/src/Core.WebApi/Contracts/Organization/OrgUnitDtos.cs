using System;

namespace MyIS.Core.WebApi.Contracts.Organization;

public class OrgUnitListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ManagerEmployeeId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public sealed class OrgUnitContactDto
{
    public Guid EmployeeId { get; init; }
    public string? EmployeeFullName { get; init; }
    public string? EmployeeEmail { get; init; }
    public string? EmployeePhone { get; init; }
    public bool IncludeInRequest { get; init; }
    public int SortOrder { get; init; }
}

public sealed class OrgUnitDetailsDto : OrgUnitListItemDto
{
    public OrgUnitContactDto[] Contacts { get; init; } = Array.Empty<OrgUnitContactDto>();
}

public sealed class OrgUnitContactRequest
{
    public Guid EmployeeId { get; init; }
    public bool IncludeInRequest { get; init; }
    public int SortOrder { get; init; }
}

public sealed class OrgUnitUpsertRequest
{
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public Guid? ParentId { get; init; }
    public Guid? ManagerEmployeeId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public OrgUnitContactRequest[] Contacts { get; init; } = Array.Empty<OrgUnitContactRequest>();
}
