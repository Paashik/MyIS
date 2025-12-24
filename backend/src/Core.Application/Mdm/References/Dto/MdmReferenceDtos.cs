using System;

namespace MyIS.Core.Application.Mdm.References.Dto;

public sealed class MdmSimpleReferenceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmItemGroupReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Abbreviation { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmUnitReferenceDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; } // Access Code (may be empty)
    public string Name { get; set; } = null!; // Access Name
    public string Symbol { get; set; } = null!; // Access Symbol
    public bool IsActive { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalId { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmSupplierReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmCustomerReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmCounterpartyRoleDto
{
    public string RoleType { get; set; } = null!; // Supplier | Customer
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmCounterpartyExternalLinkDto
{
    public string ExternalSystem { get; set; } = null!;
    public string ExternalEntity { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
    public int? SourceType { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmCounterpartyReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Site { get; set; }
    public string? SiteLogin { get; set; }
    public string? SitePassword { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MdmCounterpartyRoleDto[] Roles { get; set; } = Array.Empty<MdmCounterpartyRoleDto>();
    public MdmCounterpartyExternalLinkDto[] ExternalLinks { get; set; } = Array.Empty<MdmCounterpartyExternalLinkDto>();
}

public sealed class MdmCurrencyReferenceDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Symbol { get; set; }
    public decimal? Rate { get; set; }
    public bool IsActive { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalId { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmManufacturerReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Site { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalId { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MdmItemReferenceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string NomenclatureNo { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Designation { get; set; }
    public string ItemKind { get; set; } = null!;
    public bool IsEskd { get; set; }
    public bool? IsEskdDocument { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public bool IsActive { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalId { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureCode { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public string? UnitOfMeasureSymbol { get; set; }
    public Guid? ItemGroupId { get; set; }
    public string? ItemGroupName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

public sealed class MdmExternalEntityLinkDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string ExternalSystem { get; set; } = null!;
    public string ExternalEntity { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
    public int? SourceType { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
