using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020SnapshotReader
{
    Task<IEnumerable<Component2020Item>> ReadItemsAsync(CancellationToken cancellationToken, Guid? connectionId = null);
    Task<IEnumerable<Component2020ItemGroup>> ReadItemGroupsAsync(CancellationToken cancellationToken, Guid? connectionId = null);
    Task<IEnumerable<Component2020Unit>> ReadUnitsAsync(CancellationToken cancellationToken, Guid? connectionId = null);
    Task<IEnumerable<Component2020Attribute>> ReadAttributesAsync(CancellationToken cancellationToken, Guid? connectionId = null);
}

public class Component2020Item
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? GroupId { get; set; }
    public int? UnitId { get; set; }
    public string? PartNumber { get; set; }
    public int? ManufacturerId { get; set; }
    public string? DataSheet { get; set; }
    public bool? CanMeans { get; set; }
    public int? BomSection { get; set; }
    public byte[]? Photo { get; set; }
    // etc.
}

public class Component2020ItemGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public string? Description { get; set; }
    public string? FullName { get; set; }
}

public class Component2020Unit
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public string? Code { get; set; }
}

public class Component2020Attribute
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Symbol { get; set; }
}

public class Component2020Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Inn { get; set; }
    public string? Kpp { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Site { get; set; }
    public string? SiteLogin { get; set; }
    public string? SitePassword { get; set; }
    public int ProviderType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Bic { get; set; }
    public string? Bank { get; set; }
    public string? CorrAcc { get; set; }
    public string? Account { get; set; }
    public string? Note { get; set; }
}

public class Component2020Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? Parent { get; set; }
    public string? Project { get; set; }
    public int? GroupId { get; set; }
    public int? Kind { get; set; }
    public int? Goods { get; set; }
    public int? Own { get; set; }
    public int? Blank { get; set; }
    public int? MaterialId { get; set; }
    public decimal? MaterialQty { get; set; }
    public int? DetailId { get; set; }
    public int? Warranty { get; set; }
    public int? ProviderId { get; set; }
    public string? QrCode { get; set; }
    public int? NeedSn { get; set; }
    public bool Hidden { get; set; }
    public string? PartNumber { get; set; }
    public string? Prices { get; set; }
    public int? MinQty { get; set; }
    public DateTime? Dt { get; set; }
    public int? UserId { get; set; }
    public int? DeptId { get; set; }
    public byte[]? Photo { get; set; }
}

public class Component2020Manufacturer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Site { get; set; }
    public string? Note { get; set; }
}

public class Component2020BodyType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? Pins { get; set; }
    public int? Smt { get; set; }
    public string? Photo { get; set; }
    public string? FootPrintPath { get; set; }
    public string? FootprintRef { get; set; }
    public string? FootprintRef2 { get; set; }
    public string? FootPrintRef3 { get; set; }
}

public class Component2020Currency
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Symbol { get; set; }
    public string? Code { get; set; }
    public decimal? Rate { get; set; }
}

public class Component2020TechnicalParameter
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Symbol { get; set; }
    public int? UnitId { get; set; }
}

public class Component2020ParameterSet
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? P0Id { get; set; }
    public int? P1Id { get; set; }
    public int? P2Id { get; set; }
    public int? P3Id { get; set; }
    public int? P4Id { get; set; }
    public int? P5Id { get; set; }
}

public class Component2020Symbol
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? SymbolValue { get; set; }
    public string? Photo { get; set; }
    public string? LibraryPath { get; set; }
    public string? LibraryRef { get; set; }
}

public class Component2020Person
{
    public int Id { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? SecondName { get; set; }
    public string? Position { get; set; }
    public int? DeptId { get; set; }
    public bool Hidden { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Note { get; set; }
}

public class Component2020User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Password { get; set; }
    public bool Hidden { get; set; }
    public int? RoleId { get; set; }
    public int? PersonId { get; set; }
    public string? Roles { get; set; }
    public string? Ui { get; set; }
}

public class Component2020Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
}

public class Component2020CustomerOrder
{
    public int Id { get; set; }
    public string? Number { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int? State { get; set; }
    public int? CustomerId { get; set; }
    public string? Note { get; set; }
    public string? Contract { get; set; }
    public int? StoreId { get; set; }
    public int? PersonId { get; set; }
    public string? Path { get; set; }
    public DateTime? PayDate { get; set; }
    public DateTime? FinishedDate { get; set; }
    public int? ContactId { get; set; }
    public int? Discount { get; set; }
    public int? Tax { get; set; }
    public int? Mark { get; set; }
    public int? Pn { get; set; }
    public int? PaymentForm { get; set; }
    public int? PayMethod { get; set; }
    public int? PayPeriod { get; set; }
    public int? Prepayment { get; set; }
    public int? Kind { get; set; }
    public int? AccountId { get; set; }
}

public class Component2020Status
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? Color { get; set; }
    public int? Kind { get; set; }
    public int? Code { get; set; }
    public int? SortOrder { get; set; }
    public int? Flags { get; set; }
}
