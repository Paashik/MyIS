using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020ProductsSyncHandler : Component2020ItemSyncHandlerBase, IComponent2020SyncHandler
{
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly ILogger<Component2020ProductsSyncHandler> _logger;

    public Component2020ProductsSyncHandler(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020ProductsSyncHandler> logger)
        : base(dbContext, snapshotReader, externalLinkHelper, logger)
    {
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Products;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "Product";
        const string sourceEntity = "Products";
        const string externalSystem = "Component2020Product";
        const string externalEntity = "Product";
        const string linkEntityType = nameof(Item);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var products = (await _deltaReader.ReadProductsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        await using var transaction = !dryRun ? await DbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        var groupMappings = await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
        var itemGroupIdByExternalId = groupMappings.ItemGroupIdByExternalId;
        var rootGroupIdByExternalId = groupMappings.RootGroupIdByExternalId;
        var rootAbbreviationByExternalId = groupMappings.RootAbbreviationByExternalId;
        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);

        var sequences = new Dictionary<ItemKind, ItemSequence>();
        var dryRunSequences = new Dictionary<ItemKind, DryRunSequenceState>();

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLastId) ? parsedLastId : 0;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Item> existingItemsById;

        if (!dryRun && products.Count > 0)
        {
            var externalIds = products.Select(p => p.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await DbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await DbContext.Items
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingItemsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingItemsById = new Dictionary<Guid, Item>();
        }

        foreach (var product in products)
        {
            try
            {
                var externalId = product.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var name = string.IsNullOrWhiteSpace(product.Description) ? product.Name : product.Description;
                var designation = product.Name;

                Guid? itemGroupId = null;
                if (product.GroupId.HasValue && itemGroupIdByExternalId.TryGetValue(product.GroupId.Value, out var mappedGroupId))
                {
                    itemGroupId = mappedGroupId;
                }

                var itemKind = ResolveItemKindByGroupRoot(product.GroupId, rootGroupIdByExternalId, ItemKind.Product);

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                var prefix = ResolveNomenclaturePrefix(itemKind, product.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);

                var nomenclatureNo = existing != null && IsValidNomenclatureNo(existing.NomenclatureNo)
                    ? existing.NomenclatureNo
                    : await GenerateNextNomenclatureNoAsync(itemKind, prefix, dryRun, sequences, dryRunSequences, cancellationToken);

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var newItem = new Item(nomenclatureNo, nomenclatureNo, name, itemKind, defaultUoM.Id, itemGroupId);
                        newItem.Update(nomenclatureNo, name, defaultUoM.Id, itemGroupId, newItem.IsEskd, newItem.IsEskdDocument, designation, newItem.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        DbContext.Items.Add(newItem);
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, newItem.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.SetItemKind(itemKind);

                        var targetNomenclatureNo = IsValidNomenclatureNo(existing.NomenclatureNo) ? existing.NomenclatureNo : nomenclatureNo;
                        existing.Update(targetNomenclatureNo, name, defaultUoM.Id, itemGroupId, existing.IsEskd, existing.IsEskdDocument, designation, existing.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing product {ProductId}", product.Id);
                var error = new Component2020SyncError(runId, entityType, null, product.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            var newLastKey = newLastId.ToString(CultureInfo.InvariantCulture);
            await DbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await ExternalLinkHelper.DeleteMissingByExternalLinkAsync(
                DbContext.Items,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ProductDeleted"] = deleted;
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        counters[entityType] = processed;
        return (processed, errors);
    }
}

