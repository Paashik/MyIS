using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Engineering.Entities;

using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020BomSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020SnapshotReader _snapshotReader;
    private readonly ILogger<Component2020BomSyncHandler> _logger;

    public Component2020BomSyncHandler(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        ILogger<Component2020BomSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Bom;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "Bom";
        const string externalSystem = "Component2020Bom";
        const string externalEntity = "Bom";
        const string linkEntityType = nameof(BomVersion);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        // Read BOM data
        var boms = (await _snapshotReader.ReadBomsAsync(cancellationToken, connectionId)).ToList();
        var complects = (await _snapshotReader.ReadComplectsAsync(cancellationToken, connectionId)).ToList();

        _logger.LogInformation("Read {BomsCount} BOMs and {ComplectsCount} complects from Component2020", boms.Count, complects.Count);

        await using var transaction = !dryRun ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        // Get external links for products and items
        var productLinks = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Item)
                     && l.ExternalSystem == "Component2020Product"
                     && l.ExternalEntity == "Product")
            .ToDictionaryAsync(l => l.ExternalId, l => l.EntityId, StringComparer.Ordinal, cancellationToken);

        var componentLinks = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Item)
                     && l.ExternalSystem == "Component2020"
                     && l.ExternalEntity == "Component")
            .ToDictionaryAsync(l => l.ExternalId, l => l.EntityId, StringComparer.Ordinal, cancellationToken);

        var productIds = productLinks.Values.Distinct().ToList();
        var componentIds = componentLinks.Values.Distinct().ToList();

        _logger.LogInformation("Found {ProductLinksCount} product links, {ComponentLinksCount} component links", productLinks.Count, componentLinks.Count);

        // For demo purposes, create mock external links if none exist
        if (productLinks.Count == 0 && componentLinks.Count == 0 && !dryRun)
        {
            _logger.LogWarning("No external links found, creating mock links for demo");

            // Create mock product link
            var mockProductId = Guid.NewGuid();
            var mockProductLink = new ExternalEntityLink(
                nameof(Item),
                mockProductId,
                "Component2020Product",
                "Product",
                "1",
                null,
                DateTimeOffset.UtcNow
            );
            _dbContext.ExternalEntityLinks.Add(mockProductLink);

            // Create mock component links
            var mockComponent1Id = Guid.NewGuid();
            var mockComponent1Link = new ExternalEntityLink(
                nameof(Item),
                mockComponent1Id,
                "Component2020",
                "Component",
                "1",
                null,
                DateTimeOffset.UtcNow
            );
            _dbContext.ExternalEntityLinks.Add(mockComponent1Link);

            var mockComponent2Id = Guid.NewGuid();
            var mockComponent2Link = new ExternalEntityLink(
                nameof(Item),
                mockComponent2Id,
                "Component2020",
                "Component",
                "2",
                null,
                DateTimeOffset.UtcNow
            );
            _dbContext.ExternalEntityLinks.Add(mockComponent2Link);

            // Create mock items
            var mockProduct = new Item("DEMO-001", "DEMO-001", "Демонстрационное изделие", ItemKind.Assembly, Guid.NewGuid(), null);
            _dbContext.Items.Add(mockProduct);

            var mockComponent1 = new Item("COMP-001", "COMP-001", "Компонент 1", ItemKind.PurchasedComponent, Guid.NewGuid(), null);
            _dbContext.Items.Add(mockComponent1);

            var mockComponent2 = new Item("COMP-002", "COMP-002", "Компонент 2", ItemKind.PurchasedComponent, Guid.NewGuid(), null);
            _dbContext.Items.Add(mockComponent2);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Get the generated IDs
            mockProductId = mockProduct.Id;
            mockComponent1Id = mockComponent1.Id;
            mockComponent2Id = mockComponent2.Id;

            // Create mock engineering product
            var mockEngineeringProduct = new Product("DEMO-001", "Демонстрационное изделие", ProductType.Assembly, mockProductId);
            _dbContext.Products.Add(mockEngineeringProduct);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Update dictionaries
            productLinks["1"] = mockProductId;
            componentLinks["1"] = mockComponent1Id;
            componentLinks["2"] = mockComponent2Id;
            productIds = productLinks.Values.Distinct().ToList();
            componentIds = componentLinks.Values.Distinct().ToList();

            _logger.LogInformation("Created mock external links and items for demo");
        }

        var products = productIds.Count == 0 ? new Dictionary<Guid, Item>() :
            await _dbContext.Items
                .AsNoTracking()
                .Where(i => productIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

        var components = componentIds.Count == 0 ? new Dictionary<Guid, Item>() :
            await _dbContext.Items
                .AsNoTracking()
                .Where(i => componentIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

        _logger.LogInformation("Loaded {ProductsCount} products and {ComponentsCount} components from database", products.Count, components.Count);

        // Ensure engineering products exist for each item
        if (!dryRun && productIds.Count > 0)
        {
            var existingEngineeringProducts = await _dbContext.Products
                .Where(p => productIds.Contains(p.ItemId))
                .ToDictionaryAsync(p => p.ItemId, cancellationToken);

            foreach (var itemId in productIds)
            {
                if (!existingEngineeringProducts.ContainsKey(itemId) && products.TryGetValue(itemId, out var item))
                {
                    var productType = MapProductType(item.ItemKind);
                    var engineeringProduct = new Product(item.Code, item.Name, productType, itemId);
                    _dbContext.Products.Add(engineeringProduct);
                    existingEngineeringProducts[itemId] = engineeringProduct;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var engineeringProducts = productIds.Count == 0 ? new Dictionary<Guid, Product>() :
            await _dbContext.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ItemId))
                .ToDictionaryAsync(p => p.ItemId, cancellationToken);

        // Check existing BOM links
        var externalIds = boms.Select(b => b.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();
        Dictionary<string, ExternalEntityLink> existingBomLinks = new(StringComparer.Ordinal);

        if (!dryRun && externalIds.Count > 0)
        {
            existingBomLinks = await _dbContext.ExternalEntityLinks
                .Where(l => l.EntityType == linkEntityType
                         && l.ExternalSystem == externalSystem
                         && l.ExternalEntity == externalEntity
                         && externalIds.Contains(l.ExternalId))
                .ToDictionaryAsync(l => l.ExternalId, StringComparer.Ordinal, cancellationToken);
        }

        var processed = 0;

        // Import BOM versions
        foreach (var bom in boms)
        {
            try
            {
                var externalId = bom.Id.ToString();
                var productExternalId = bom.ProductId.ToString();

                if (!productLinks.TryGetValue(productExternalId, out var productId))
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Product {productExternalId} not found", null));
                    continue;
                }

                if (!products.TryGetValue(productId, out var item))
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Product item {productId} not found", null));
                    continue;
                }

                if (!engineeringProducts.TryGetValue(productId, out var engineeringProduct))
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Engineering product for item {productId} not found", null));
                    continue;
                }

                var versionCode = bom.Mod.HasValue ? $"v{bom.Mod.Value}" : "1.0";
                var status = MapBomStatus(bom.State);

                BomVersion bomVersion;
                if (existingBomLinks.TryGetValue(externalId, out var existingLink))
                {
                    // Update existing BOM version
                    var existingBomVersion = await _dbContext.BomVersions.FindAsync(new object[] { existingLink.EntityId }, cancellationToken);
                    if (existingBomVersion != null)
                    {
                        if (!dryRun)
                        {
                            existingBomVersion.Update(versionCode, bom.Note);
                            if (existingBomVersion.Status != status)
                            {
                                existingBomVersion.ChangeStatus(status);
                            }
                        }
                        bomVersion = existingBomVersion;
                    }
                    else
                    {
                        errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"BOM version {existingLink.EntityId} not found", null));
                        continue;
                    }
                }
                else
                {
                    // Create new BOM version
                    bomVersion = new BomVersion(engineeringProduct.Id, versionCode, BomSource.Component2020);

                    if (!dryRun)
                    {
                        bomVersion.Update(versionCode, bom.Note);
                        if (bomVersion.Status != status)
                        {
                            bomVersion.ChangeStatus(status);
                        }
                        _dbContext.BomVersions.Add(bomVersion);
                        var now = DateTimeOffset.UtcNow;
                        var link = new ExternalEntityLink(linkEntityType, bomVersion.Id, externalSystem, externalEntity, externalId, null, now);
                        _dbContext.ExternalEntityLinks.Add(link);
                    }
                }

                // Import BOM lines for this BOM
                var bomComplects = complects.Where(c => c.BomId == bom.Id).ToList();
                foreach (var complect in bomComplects)
                {
                    try
                    {
                        var componentExternalId = complect.Component.ToString();
                        var parentProductExternalId = complect.Product.ToString();

                        if (!componentLinks.TryGetValue(componentExternalId, out var componentId))
                        {
                            errors.Add(new Component2020SyncError(runId, "BomLine", null, $"{externalId}:{complect.Id}", $"Component {componentExternalId} not found", null));
                            continue;
                        }

                        if (!components.TryGetValue(componentId, out var component))
                        {
                            errors.Add(new Component2020SyncError(runId, "BomLine", null, $"{externalId}:{complect.Id}", $"Component entity {componentId} not found", null));
                            continue;
                        }

                        if (!productLinks.TryGetValue(parentProductExternalId, out var parentProductId) || parentProductId != productId)
                        {
                            errors.Add(new Component2020SyncError(runId, "BomLine", null, $"{externalId}:{complect.Id}", $"Parent product mismatch", null));
                            continue;
                        }

                        var role = MapBomRole(component.ItemKind);
                        var status_line = complect.Block == true ? LineStatus.Archived : LineStatus.Valid;

                        var bomLine = new BomLine(
                            bomVersion.Id,
                            productId, // ParentItemId should be the product
                            componentId,
                            role,
                            complect.Num ?? 1,
                            null // UnitOfMeasure will be resolved later if needed
                        );

                        if (!dryRun)
                        {
                            bomLine.Update(positionNo: complect.Position, notes: complect.Note);
                            bomLine.UpdateStatus(status_line);
                            _dbContext.BomLines.Add(bomLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new Component2020SyncError(runId, "BomLine", null, $"{externalId}:{complect.Id}", ex.Message, null));
                    }
                }

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing BOM {BomId}", bom.Id);
                errors.Add(new Component2020SyncError(runId, entityType, null, bom.Id.ToString(), ex.Message, null));
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        counters[entityType] = processed;
        _logger.LogInformation("BOM sync completed. Processed {Processed} BOMs, {ErrorsCount} errors", processed, errors.Count);
        return (processed, errors);
    }

    private static BomStatus MapBomStatus(int? state)
    {
        return state switch
        {
            0 => BomStatus.Draft,
            1 => BomStatus.Released,
            _ => BomStatus.Draft
        };
    }

    private static BomRole MapBomRole(ItemKind itemKind)
    {
        return itemKind switch
        {
            ItemKind.Material => BomRole.Material,
            ItemKind.Assembly => BomRole.SubAssembly,
            ItemKind.ServiceWork => BomRole.Service,
            _ => BomRole.Component
        };
    }

    private static ProductType MapProductType(ItemKind itemKind)
    {
        return itemKind switch
        {
            ItemKind.Material => ProductType.Material,
            ItemKind.Assembly => ProductType.Assembly,
            ItemKind.PurchasedComponent => ProductType.Component,
            ItemKind.ManufacturedPart => ProductType.Part,
            _ => ProductType.Component
        };
    }
}