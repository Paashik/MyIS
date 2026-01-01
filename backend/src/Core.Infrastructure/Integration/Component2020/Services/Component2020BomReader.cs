using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Application.Engineering.Abstractions;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public class Component2020BomReader : IComponent2020BomReader
{
    private readonly IComponent2020SnapshotReader _snapshotReader;
    private readonly IItemRepository _itemRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<Component2020BomReader> _logger;

    public Component2020BomReader(
        IComponent2020SnapshotReader snapshotReader,
        IItemRepository itemRepository,
        IProductRepository productRepository,
        ILogger<Component2020BomReader> logger)
    {
        _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<EbomVersionDto>> GetBomVersionsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading BOM versions from Component2020 (connectionId={ConnectionId})", connectionId);

        var boms = await _snapshotReader.ReadBomsAsync(cancellationToken, connectionId);
        var products = await _snapshotReader.ReadProductsAsync(cancellationToken, connectionId);

        var productDict = products.ToDictionary(p => p.Id, p => p);

        var result = new List<EbomVersionDto>();

        foreach (var bom in boms)
        {
            if (!productDict.TryGetValue(bom.ProductId, out var product))
            {
                _logger.LogWarning("Product {ProductId} not found for BOM {BomId}", bom.ProductId, bom.Id);
                continue;
            }

            // Find product UUID by external ID
            var productItem = await _itemRepository.FindByExternalAsync("Component2020", product.Id.ToString());
            if (productItem == null)
            {
                _logger.LogWarning("Product item not found for external ID {ExternalId}", product.Id);
                continue;
            }

            var versionCode = bom.Mod.HasValue ? $"v{bom.Mod.Value}" : "1.0";
            var status = MapBomStatus(bom.State);

            result.Add(new EbomVersionDto
            {
                Id = Guid.NewGuid(), // Generate new UUID for EBOM version
                ProductId = productItem.Id,
                VersionCode = versionCode,
                Status = status,
                Source = "Component2020",
                Description = bom.Note
            });
        }

        _logger.LogInformation("Successfully mapped {Count} BOM versions", result.Count);
        return result;
    }

    public async Task<IEnumerable<EbomLineDto>> GetBomLinesAsync(Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading BOM lines from Component2020 (connectionId={ConnectionId})", connectionId);

        var complects = await _snapshotReader.ReadComplectsAsync(cancellationToken, connectionId);
        var components = await _snapshotReader.ReadItemsAsync(cancellationToken, connectionId);
        var units = await _snapshotReader.ReadUnitsAsync(cancellationToken, connectionId);

        var componentDict = components.ToDictionary(c => c.Id, c => c);
        var unitDict = units.ToDictionary(u => u.Id, u => u);

        var result = new List<EbomLineDto>();

        foreach (var complect in complects)
        {
            if (!componentDict.TryGetValue(complect.Component, out var component))
            {
                _logger.LogWarning("Component {ComponentId} not found for Complect {ComplectId}", complect.Component, complect.Id);
                continue;
            }

            // Find component UUID by external ID
            var componentItem = await _itemRepository.FindByExternalAsync("Component2020", component.Id.ToString());
            if (componentItem == null)
            {
                _logger.LogWarning("Component item not found for external ID {ExternalId}", component.Id);
                continue;
            }

            // Find parent item (product) UUID by external ID
            var parentItem = await _itemRepository.FindByExternalAsync("Component2020", complect.Product.ToString());
            if (parentItem == null)
            {
                _logger.LogWarning("Parent item not found for external ID {ExternalId}", complect.Product);
                continue;
            }

            // Map BOM version from BomId in complect
            var bomVersionId = Guid.Empty; // Will be set below

            if (complect.BomId.HasValue)
            {
                // TODO: Implement proper BOM version mapping using external links or cache
                // For now, generate new ID - this should be fixed in sync handler
                bomVersionId = Guid.NewGuid();
            }
            else
            {
                // Fallback for complects without BomId
                bomVersionId = Guid.NewGuid();
            }

            var role = MapBomRole(component.BomSection);
            var unitCode = component.UnitId.HasValue && unitDict.TryGetValue(component.UnitId.Value, out var unit)
                ? unit.Code ?? "pcs"
                : "pcs";
            var status = MapLineStatus(complect.Block);

            result.Add(new EbomLineDto
            {
                Id = Guid.NewGuid(),
                BomVersionId = bomVersionId,
                ParentItemId = parentItem.Id,
                ItemId = componentItem.Id,
                Role = role,
                Quantity = complect.Num ?? 1,
                UnitOfMeasure = unitCode,
                PositionNo = complect.Position,
                Notes = complect.Note,
                Status = status
            });
        }

        _logger.LogInformation("Successfully mapped {Count} BOM lines", result.Count);
        return result;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading products from Component2020 (connectionId={ConnectionId})", connectionId);

        var products = await _snapshotReader.ReadProductsAsync(cancellationToken, connectionId);

        var result = new List<ProductDto>();

        foreach (var product in products)
        {
            var productItem = await _itemRepository.FindByExternalAsync("Component2020", product.Id.ToString());
            if (productItem == null)
            {
                _logger.LogWarning("Product item not found for external ID {ExternalId}", product.Id);
                continue;
            }

            result.Add(new ProductDto
            {
                Id = productItem.Id,
                Code = product.Name?.Length <= 50 ? product.Name : null,
                Name = product.Description ?? product.Name ?? $"Product_{product.Id}",
                Description = product.Description,
                Type = MapProductType(product.Kind)
            });
        }

        _logger.LogInformation("Successfully mapped {Count} products", result.Count);
        return result;
    }

    public async Task<IEnumerable<ItemDto>> GetItemsAsync(Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading items from Component2020 (connectionId={ConnectionId})", connectionId);

        var items = await _snapshotReader.ReadItemsAsync(cancellationToken, connectionId);

        var result = new List<ItemDto>();

        foreach (var item in items)
        {
            var itemEntity = await _itemRepository.FindByExternalAsync("Component2020", item.Id.ToString());
            if (itemEntity == null)
            {
                _logger.LogWarning("Item not found for external ID {ExternalId}", item.Id);
                continue;
            }

            result.Add(new ItemDto
            {
                Id = itemEntity.Id,
                Code = item.Code,
                Name = item.Name,
                ItemType = MapItemType(item.BomSection),
                GroupName = null, // TODO: Map from groups if needed
                IsActive = true // Assume active if not hidden
            });
        }

        _logger.LogInformation("Successfully mapped {Count} items", result.Count);
        return result;
    }

    private static string MapBomStatus(int? state)
    {
        return state switch
        {
            0 => "Draft",
            1 => "Released",
            _ => "Draft"
        };
    }

    private static string MapBomRole(int? bomSection)
    {
        return bomSection switch
        {
            0 or 1 => "Component",
            3 => "Material",
            _ => "Component"
        };
    }

    private static string MapLineStatus(bool? block)
    {
        return block == true ? "Archived" : "Valid";
    }

    private static int MapProductType(int? kind)
    {
        return kind switch
        {
            0 => 1, // Assembly
            1 => 2, // Part
            2 => 3, // Complex
            _ => 1
        };
    }

    private static int MapItemType(int? bomSection)
    {
        return bomSection switch
        {
            0 or 1 => 1, // Component
            3 => 2, // Material
            _ => 1
        };
    }
}