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
using MyIS.Core.Domain.Mdm.Services;
using MyIS.Core.Domain.Mdm.ValueObjects;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020ItemsSyncHandler : Component2020ItemSyncHandlerBase, IComponent2020SyncHandler
{
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly ILogger<Component2020ItemsSyncHandler> _logger;

    public Component2020ItemsSyncHandler(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020ItemsSyncHandler> logger)
        : base(dbContext, snapshotReader, externalLinkHelper, logger)
    {
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Items;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "Item";
        const string sourceEntity = "Items";
        const string externalSystem = "Component2020";
        const string externalEntity = "Component";
        const string linkEntityType = nameof(Item);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var items = (await _deltaReader.ReadItemsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        await using var transaction = !dryRun ? await DbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        var groupMappings = await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
        var itemGroupIdByExternalId = groupMappings.ItemGroupIdByExternalId;
        var rootGroupIdByExternalId = groupMappings.RootGroupIdByExternalId;
        var rootAbbreviationByExternalId = groupMappings.RootAbbreviationByExternalId;
        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);

        var unitExternalIds = items
            .Where(x => x.UnitId.HasValue)
            .Select(x => x.UnitId!.Value.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var unitOfMeasureIdByExternalId = unitExternalIds.Count > 0
            ? (await DbContext.ExternalEntityLinks
                    .AsNoTracking()
                    .Where(l =>
                        l.EntityType == nameof(UnitOfMeasure)
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == "Unit"
                        && unitExternalIds.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(l => l.ExternalId, l => l.EntityId, StringComparer.Ordinal)
            : new Dictionary<string, Guid>(StringComparer.Ordinal);

        var sequences = new Dictionary<ItemKind, ItemSequence>();
        var dryRunSequences = new Dictionary<ItemKind, DryRunSequenceState>();

        var prefixByKind = new Dictionary<ItemKind, string>();
        var maxIncomingCodeByKind = new Dictionary<ItemKind, int>();
        foreach (var item in items)
        {
            var kind = ResolveItemKindByGroupRoot(item.GroupId, rootGroupIdByExternalId, ItemKind.Component);
            var prefix = ResolveNomenclaturePrefix(kind, item.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);
            var itemCode = NormalizeOptional(item.Code);

            if (!prefixByKind.TryGetValue(kind, out var existingPrefix))
            {
                prefixByKind[kind] = prefix;
            }
            else if (!string.Equals(existingPrefix, prefix, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Multiple nomenclature prefixes detected for ItemKind={ItemKind} during Component2020 sync (keeping '{Prefix}', ignoring '{OtherPrefix}')",
                    kind,
                    existingPrefix,
                    prefix);
            }

            if (ItemNomenclature.TryParseComponentCode(itemCode, out var codeNumber))
            {
                maxIncomingCodeByKind[kind] = Math.Max(maxIncomingCodeByKind.GetValueOrDefault(kind), codeNumber);
            }
        }

        if (!dryRun)
        {
            foreach (var (kind, prefix) in prefixByKind)
            {
                var sequence = await GetOrCreateSequenceAsync(kind, prefix, sequences, cancellationToken);
                if (maxIncomingCodeByKind.TryGetValue(kind, out var maxCodeNumber))
                {
                    sequence.EnsureNextNumberAtLeast(maxCodeNumber + 1);
                }
            }
        }

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLastId) ? parsedLastId : 0;

        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Item> existingItemsById;

        if (!dryRun && items.Count > 0)
        {
            var externalIds = items.Select(i => i.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

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

        foreach (var item in items)
        {
            try
            {
                var externalId = item.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var unitOfMeasureId = defaultUoM.Id;
                if (item.UnitId.HasValue && unitOfMeasureIdByExternalId.TryGetValue(item.UnitId.Value.ToString(), out var mappedUoMId))
                {
                    unitOfMeasureId = mappedUoMId;
                }

                var itemKind = ResolveItemKindByGroupRoot(item.GroupId, rootGroupIdByExternalId, ItemKind.Component);
                var itemCode = NormalizeOptional(item.Code);

                Guid? itemGroupId = null;
                if (item.GroupId.HasValue && itemGroupIdByExternalId.TryGetValue(item.GroupId.Value, out var mappedGroupId))
                {
                    itemGroupId = mappedGroupId;
                }

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                var prefix = ResolveNomenclaturePrefix(itemKind, item.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);

                string nomenclatureNo;
                if (ItemNomenclature.TryParseComponentCode(itemCode, out var codeNumber))
                {
                    nomenclatureNo = ItemNomenclature.FormatNomenclatureNo(prefix, codeNumber);
                }
                else
                {
                    nomenclatureNo = await GenerateNextNomenclatureNoAsync(itemKind, prefix, dryRun, sequences, dryRunSequences, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var code = itemCode ?? nomenclatureNo;
                        var newItem = new Item(code, nomenclatureNo, item.Name, itemKind, unitOfMeasureId, itemGroupId);
                        newItem.Update(nomenclatureNo, item.Name, unitOfMeasureId, itemGroupId, newItem.IsEskd, newItem.IsEskdDocument, newItem.Designation, item.PartNumber);
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
                        existing.Update(targetNomenclatureNo, item.Name, unitOfMeasureId, itemGroupId, existing.IsEskd, existing.IsEskdDocument, existing.Designation, item.PartNumber);
                        var now = DateTimeOffset.UtcNow;
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item {ItemId}", item.Id);
                var error = new Component2020SyncError(runId, entityType, null, item.Id.ToString(CultureInfo.InvariantCulture), ex.Message, ex.StackTrace);
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
            counters["ItemDeleted"] = deleted;
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        counters[entityType] = processed;
        return (processed, errors);
    }
}

