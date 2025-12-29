using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.Services;
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

    private static readonly Regex DesignationPattern =
        new(@"\b[А-ЯA-Z0-9]{2,10}\.\d{6}\.\d{3}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] PurchasedComponentKeywords =
    {
        "микросхем",
        "резистор",
        "конденсатор",
        "разъем",
        "разъём",
        "фильтр",
        "актив",
        "пассив"
    };

    private static readonly string[] MaterialKeywords =
    {
        "материал",
        "сырье",
        "сырьё",
        "хим",
        "припой",
        "клей",
        "герметик",
        "фольг"
    };

    private static readonly string[] StandardPartKeywords =
    {
        "крепеж",
        "крепёж",
        "винт",
        "гайк",
        "шайб",
        "болт"
    };

    private static readonly string[] ServiceKeywords =
    {
        "работ",
        "услуг"
    };

    private static readonly string[] ManufacturedPartKeywords =
    {
        "детал",
        "заготов",
        "механо",
        "обработка"
    };

    private static string? NormalizeDesignationKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static IReadOnlyList<string> ExtractDesignationCandidates(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return Array.Empty<string>();
        }

        var matches = DesignationPattern.Matches(source);
        if (matches.Count == 0)
        {
            return Array.Empty<string>();
        }

        return matches
            .Select(m => m.Value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractDesignationCandidates(Component2020Item item)
    {
        var fromPartNumber = ExtractDesignationCandidates(item.PartNumber);
        if (fromPartNumber.Count > 0)
        {
            return fromPartNumber;
        }

        var fromName = ExtractDesignationCandidates(item.Name);
        if (fromName.Count > 0)
        {
            return fromName;
        }

        return ExtractDesignationCandidates(item.Description);
    }

    private static bool ContainsAny(string? source, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        foreach (var keyword in keywords)
        {
            if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasGostOrTu(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return source.IndexOf("ГОСТ", StringComparison.OrdinalIgnoreCase) >= 0
               || source.IndexOf("ТУ", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsMaterialUnit(UnitOfMeasure? unit)
    {
        if (unit == null)
        {
            return false;
        }

        var symbol = unit.Symbol ?? string.Empty;
        var name = unit.Name ?? string.Empty;

        return symbol.Equals("кг", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("кг.", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("kg", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("м", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("м.", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("m", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("л", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("л.", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("l", StringComparison.OrdinalIgnoreCase)
               || name.IndexOf("кг", StringComparison.OrdinalIgnoreCase) >= 0
               || name.IndexOf("килограмм", StringComparison.OrdinalIgnoreCase) >= 0
               || name.IndexOf("литр", StringComparison.OrdinalIgnoreCase) >= 0
               || name.IndexOf("метр", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ResolveComponentName(string? source, string? designation)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return source ?? string.Empty;
        }

        var name = source.Trim();
        if (string.IsNullOrWhiteSpace(designation))
        {
            return name;
        }

        var trimmedDesignation = designation.Trim();
        if (name.StartsWith(trimmedDesignation, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = name.Substring(trimmedDesignation.Length);
            remainder = remainder.TrimStart();
            if (!string.IsNullOrWhiteSpace(remainder))
            {
                return remainder;
            }
        }

        return name;
    }

    private static ItemKind ResolveComponentItemKind(Component2020Item item, string? groupName, UnitOfMeasure? unit, string? designation)
    {
        if (!string.IsNullOrWhiteSpace(designation))
        {
            return ItemKind.ManufacturedPart;
        }

        if (item.BomSection == 3)
        {
            return ItemKind.Material;
        }

        if (item.BomSection == 1)
        {
            return ItemKind.StandardPart;
        }

        if (ContainsAny(groupName, ServiceKeywords))
        {
            return ItemKind.ServiceWork;
        }

        if (ContainsAny(groupName, MaterialKeywords) || IsMaterialUnit(unit))
        {
            return ItemKind.Material;
        }

        if (ContainsAny(groupName, StandardPartKeywords)
            || HasGostOrTu(item.Name)
            || HasGostOrTu(item.Description)
            || HasGostOrTu(item.PartNumber))
        {
            return ItemKind.StandardPart;
        }

        if (ContainsAny(groupName, ManufacturedPartKeywords))
        {
            return ItemKind.ManufacturedPart;
        }

        if (item.ManufacturerId.HasValue
            || !string.IsNullOrWhiteSpace(item.PartNumber)
            || !string.IsNullOrWhiteSpace(item.DataSheet)
            || ContainsAny(groupName, PurchasedComponentKeywords))
        {
            return ItemKind.PurchasedComponent;
        }

        return ItemKind.PurchasedComponent;
    }

    private static void AddReviewError(
        List<Component2020SyncError> errors,
        Guid runId,
        int componentId,
        string reason,
        string? designation,
        IEnumerable<Item>? productCandidates)
    {
        var candidateIds = productCandidates?.Select(c => c.Id.ToString()).ToList() ?? new List<string>();
        var message = $"Review required: {reason}. ComponentId={componentId}.";
        var details = candidateIds.Count > 0
            ? $"Designation='{designation}', ProductIds=[{string.Join(",", candidateIds)}]"
            : $"Designation='{designation}'";

        errors.Add(new Component2020SyncError(runId, "ItemMatchReview", "Component", componentId.ToString(CultureInfo.InvariantCulture), message, details));
    }

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
        var groupNameByExternalId = groupMappings.GroupNameByExternalId;
        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);
        var defaultGroupIds = await LoadDefaultItemGroupIdsAsync(cancellationToken);
        var rootAbbreviationByGroupId = await LoadGroupRootAbbreviationsAsync(cancellationToken);

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

        var unitIds = unitOfMeasureIdByExternalId.Values.Distinct().ToList();
        var unitsById = unitIds.Count > 0
            ? await DbContext.UnitOfMeasures
                .AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken)
            : new Dictionary<Guid, UnitOfMeasure>();

        var sequences = new Dictionary<ItemKind, ItemSequence>();
        var dryRunSequences = new Dictionary<ItemKind, DryRunSequenceState>();

        var prefixByKind = new Dictionary<ItemKind, string>();
        var maxIncomingCodeByKind = new Dictionary<ItemKind, int>();
        foreach (var item in items)
        {
            var groupName = item.GroupId.HasValue && groupNameByExternalId.TryGetValue(item.GroupId.Value, out var groupLabel)
                ? groupLabel
                : null;

            UnitOfMeasure? unit = null;
            if (item.UnitId.HasValue
                && unitOfMeasureIdByExternalId.TryGetValue(item.UnitId.Value.ToString(), out var mappedUnitId))
            {
                unitsById.TryGetValue(mappedUnitId, out unit);
            }

            var designationCandidates = ExtractDesignationCandidates(item);
            var componentDesignation = designationCandidates.Count == 1 ? designationCandidates[0] : null;
            var kind = ResolveComponentItemKind(item, groupName, unit, componentDesignation);
            var prefix = ItemNomenclature.GetDefaultPrefix(kind);
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
        Dictionary<string, Item> existingItemsByCode;

        if (items.Count > 0)
        {
            var externalIds = items.Select(i => i.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();
            var incomingCodes = items
                .Select(i => NormalizeOptional(i.Code))
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var linkQuery = DbContext.ExternalEntityLinks.AsQueryable();
            if (dryRun)
            {
                linkQuery = linkQuery.AsNoTracking();
            }

            var existingLinks = await linkQuery
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var itemQuery = DbContext.Items.AsQueryable();
            if (dryRun)
            {
                itemQuery = itemQuery.AsNoTracking();
            }

            var existingEntities = await itemQuery
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingItemsById = existingEntities.ToDictionary(x => x.Id);
            existingItemsByCode = incomingCodes.Count > 0
                ? await itemQuery
                    .Where(x => incomingCodes.Contains(x.Code))
                    .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken)
                : new Dictionary<string, Item>(StringComparer.Ordinal);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingItemsById = new Dictionary<Guid, Item>();
            existingItemsByCode = new Dictionary<string, Item>(StringComparer.Ordinal);
        }

        var productItemsByDesignation = new Dictionary<string, List<Item>>(StringComparer.Ordinal);
        var productItemIds = new HashSet<Guid>();

        if (items.Count > 0)
        {
            var productLinkQuery = DbContext.ExternalEntityLinks.AsQueryable();
            if (dryRun)
            {
                productLinkQuery = productLinkQuery.AsNoTracking();
            }

            var productLinks = await productLinkQuery
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == "Component2020Product"
                    && l.ExternalEntity == "Product")
                .ToListAsync(cancellationToken);

            var productIds = productLinks
                .Select(l => l.EntityId)
                .Distinct()
                .ToList();

            if (productIds.Count > 0)
            {
                productItemIds = new HashSet<Guid>(productIds);
                var productItemQuery = DbContext.Items.AsQueryable();
                if (dryRun)
                {
                    productItemQuery = productItemQuery.AsNoTracking();
                }

                var productItems = await productItemQuery
                    .Where(x => productIds.Contains(x.Id))
                    .ToListAsync(cancellationToken);

                productItemsByDesignation = productItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.Designation))
                    .GroupBy(x => NormalizeDesignationKey(x.Designation)!)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            }
        }

        foreach (var item in items)
        {
            try
            {
                var externalId = item.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var unitOfMeasureId = defaultUoM.Id;
                UnitOfMeasure? unit = null;
                if (item.UnitId.HasValue && unitOfMeasureIdByExternalId.TryGetValue(item.UnitId.Value.ToString(), out var mappedUoMId))
                {
                    unitOfMeasureId = mappedUoMId;
                    unitsById.TryGetValue(mappedUoMId, out unit);
                }

                var groupName = item.GroupId.HasValue && groupNameByExternalId.TryGetValue(item.GroupId.Value, out var groupLabel)
                    ? groupLabel
                    : null;

                var designationCandidates = ExtractDesignationCandidates(item);
                var componentDesignation = designationCandidates.Count == 1 ? designationCandidates[0] : null;

                if (designationCandidates.Count > 1)
                {
                    AddReviewError(
                        errors,
                        runId,
                        item.Id,
                        "Multiple designation candidates in Component",
                        string.Join(";", designationCandidates),
                        null);
                }

                var itemKind = ResolveComponentItemKind(item, groupName, unit, componentDesignation);
                var isTooling = item.CanMeans == true;
                var itemCode = NormalizeOptional(item.Code);

                Guid? itemGroupId = null;
                if (item.GroupId.HasValue && itemGroupIdByExternalId.TryGetValue(item.GroupId.Value, out var mappedGroupId))
                {
                    itemGroupId = mappedGroupId;
                }
                if (string.IsNullOrWhiteSpace(componentDesignation) || !itemGroupId.HasValue)
                {
                    itemGroupId = ResolveItemGroupIdForKind(itemGroupId, itemKind, rootAbbreviationByGroupId, defaultGroupIds);
                }

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }
                else
                {
                    var designationKey = NormalizeDesignationKey(componentDesignation);
                    if (designationKey != null && productItemsByDesignation.TryGetValue(designationKey, out var candidates))
                    {
                        if (candidates.Count == 1)
                        {
                            var candidate = candidates[0];
                            if (candidate.ItemKind != itemKind)
                            {
                                existing = candidate;
                                _logger.LogWarning(
                                    "Classification conflict resolved by Product priority. ComponentId={ComponentId}, ProductItemId={ItemId}, ComponentKind={ComponentKind}, ProductKind={ProductKind}",
                                    item.Id,
                                    candidate.Id,
                                    itemKind,
                                    candidate.ItemKind);
                            }
                            else
                            {
                                existing = candidate;
                            }
                        }
                        else if (candidates.Count > 1)
                        {
                            AddReviewError(errors, runId, item.Id, "Multiple Product candidates", componentDesignation, candidates);
                        }
                    }

                    if (existing == null && !string.IsNullOrWhiteSpace(itemCode) && existingItemsByCode.TryGetValue(itemCode, out var existingByCode))
                    {
                        existing = existingByCode;
                        _logger.LogWarning(
                            "Component2020 item matched by code without external link. Code={ItemCode}, ExternalId={ExternalId}, ExistingId={ExistingId}",
                            itemCode,
                            externalId,
                            existingByCode.Id);
                    }
                }

                var prefix = ItemNomenclature.GetDefaultPrefix(itemKind);
                var resolvedName = ResolveComponentName(item.Name, componentDesignation);

                string nomenclatureNo;
                if (ItemNomenclature.TryParseComponentCode(itemCode, out var codeNumber))
                {
                    nomenclatureNo = ItemNomenclature.FormatNomenclatureNo(prefix, codeNumber);
                }
                else
                {
                    nomenclatureNo = await GenerateNextNomenclatureNoAsync(itemKind, prefix, dryRun, sequences, dryRunSequences, cancellationToken);
                }
                var codeToUse = itemCode ?? nomenclatureNo;

                if (existing == null)
                {
                    if (!dryRun && existingItemsByCode.TryGetValue(codeToUse, out var existingByGeneratedCode))
                    {
                        existing = existingByGeneratedCode;
                    }
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var newItem = new Item(codeToUse, nomenclatureNo, resolvedName, itemKind, unitOfMeasureId, itemGroupId);
                        newItem.Update(
                            nomenclatureNo,
                            resolvedName,
                            unitOfMeasureId,
                            itemGroupId,
                            newItem.IsEskd,
                            newItem.IsEskdDocument,
                            componentDesignation,
                            item.PartNumber,
                            isTooling,
                            false);
                        newItem.SetPhoto(item.Photo);
                        var now = DateTimeOffset.UtcNow;
                        DbContext.Items.Add(newItem);
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, newItem.Id, externalSystem, externalEntity, externalId, null, now);
                        existingItemsByCode[codeToUse] = newItem;
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        var isProductItem = productItemIds.Contains(existing.Id);
                        var targetNomenclatureNo = IsValidNomenclatureNo(existing.NomenclatureNo) ? existing.NomenclatureNo : nomenclatureNo;

                        var manufacturerPartNumber = !string.IsNullOrWhiteSpace(item.PartNumber)
                            ? item.PartNumber
                            : existing.ManufacturerPartNumber;
                        if (isProductItem && !string.IsNullOrWhiteSpace(existing.ManufacturerPartNumber))
                        {
                            manufacturerPartNumber = existing.ManufacturerPartNumber;
                        }

                        if (isProductItem)
                        {
                            existing.Update(
                                existing.NomenclatureNo,
                                existing.Name,
                                existing.UnitOfMeasureId,
                                existing.ItemGroupId,
                                existing.IsEskd,
                                existing.IsEskdDocument,
                                existing.Designation,
                                manufacturerPartNumber,
                                existing.IsTooling || isTooling,
                                existing.IsFinishedProduct);
                        }
                        else
                        {
                            existing.SetItemKind(itemKind);
                            existing.Update(
                                targetNomenclatureNo,
                                resolvedName,
                                unitOfMeasureId,
                                itemGroupId,
                                existing.IsEskd,
                                existing.IsEskdDocument,
                                componentDesignation ?? existing.Designation,
                                manufacturerPartNumber,
                                isTooling,
                                existing.IsFinishedProduct);
                        }
                        existing.SetPhoto(item.Photo);

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

