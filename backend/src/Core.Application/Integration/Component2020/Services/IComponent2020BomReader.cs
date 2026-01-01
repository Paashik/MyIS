using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020BomReader
{
    Task<IEnumerable<EbomVersionDto>> GetBomVersionsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<EbomLineDto>> GetBomLinesAsync(Guid? connectionId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductDto>> GetProductsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ItemDto>> GetItemsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default);
}

public class EbomVersionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string VersionCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Source { get; set; } = "Component2020";
    public string? Description { get; set; }
}

public class EbomLineDto
{
    public Guid Id { get; set; }
    public Guid BomVersionId { get; set; }
    public Guid ParentItemId { get; set; }
    public Guid ItemId { get; set; }
    public string Role { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public string? PositionNo { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = null!;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Type { get; set; }
}

public class ItemDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public int ItemType { get; set; }
    public string? GroupName { get; set; }
    public bool IsActive { get; set; }
}