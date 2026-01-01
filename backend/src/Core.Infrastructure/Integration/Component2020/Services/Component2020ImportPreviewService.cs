using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public sealed class Component2020ImportPreviewService : Component2020ItemSyncHandlerBase, IComponent2020ImportPreviewService
{
    private const string ComponentExternalSystem = "Component2020";
    private const string ComponentExternalEntity = "Component";
    private const string ProductExternalSystem = "Component2020Product";
    private const string ProductExternalEntity = "Product";

    private static readonly Regex DesignationPattern =
        new(@"\b[А-ЯA-Z0-9]{2,10}\.\d{6}\.\d{3}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] PurchasedComponentKeywords =
    {
        "микросхем",
        "резистор",
        "конденсатор",
        "разъём",
        "разъем",
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
        "обработк"
    };

    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly ILogger<Component2020ImportPreviewService> _logger;

    public Component2020ImportPreviewService(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020ImportPreviewService> logger)
        : base(dbContext, snapshotReader, externalLinkHelper, logger)
    {
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Component2020ImportPreviewResponseDto> PreviewAsync(
        Component2020ImportPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Page <= 0)
        {
            request.Page = 1;
        }

        if (request.PageSize <= 0)
        {
            request.PageSize = 200;
        }

        var isFull = request.SyncMode != Component2020SyncMode.Delta;
        var productsLastKey = isFull
            ? null
            : await _cursorRepository.GetLastProcessedKeyAsync(request.ConnectionId, "Products", cancellationToken);
        var itemsLastKey = isFull
            ? null
            : await _cursorRepository.GetLastProcessedKeyAsync(request.ConnectionId, "Items", cancellationToken);

        var components = (await _deltaReader.ReadItemsDeltaAsync(request.ConnectionId, itemsLastKey, cancellationToken)).ToList();
        var products = (await _deltaReader.ReadProductsDeltaAsync(request.ConnectionId, productsLastKey, cancellationToken)).ToList();

        var externalGroups = (await SnapshotReader.ReadItemGroupsAsync(cancellationToken, request.ConnectionId))
            .GroupBy(g => g.Id)
            .Select(g => g.First())
            .ToDictionary(g => g.Id, g => g.Name ?? string.Empty);

        var groupLinks = await DbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l =>
                l.EntityType == nameof(ItemGroup)
                && l.ExternalSystem == ComponentExternalSystem
                && l.ExternalEntity == "Groups")
            .ToListAsync(cancellationToken);

        var groupIdByExternalId = groupLinks
            .GroupBy(l => l.ExternalId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().EntityId, StringComparer.Ordinal);

        var groupIds = groupIdByExternalId.Values.Distinct().ToList();
        var groupsById = groupIds.Count == 0
            ? new Dictionary<Guid, ItemGroup>()
            : await DbContext.ItemGroups
                .AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, cancellationToken);

        var rootAbbreviationByGroupId = await LoadGroupRootAbbreviationsAsync(cancellationToken);
        var defaultGroupIds = await LoadDefaultItemGroupIdsAsync(cancellationToken);
        var assemblySubgroupIds = await LoadAssemblySubgroupIdsAsync(defaultGroupIds, cancellationToken);

        var fallbackGroupIds = defaultGroupIds.Values
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (fallbackGroupIds.Count > 0)
        {
            var missingFallbackIds = fallbackGroupIds.Where(id => !groupsById.ContainsKey(id)).ToList();
            if (missingFallbackIds.Count > 0)
            {
                var fallbackGroups = await DbContext.ItemGroups
                    .AsNoTracking()
                    .Where(g => missingFallbackIds.Contains(g.Id))
                    .ToListAsync(cancellationToken);
                foreach (var group in fallbackGroups)
                {
                    groupsById[group.Id] = group;
                }
            }
        }

        if (assemblySubgroupIds.Count > 0)
        {
            var subgroupIds = assemblySubgroupIds.Values.Distinct().ToList();
            var missingSubgroupIds = subgroupIds.Where(id => !groupsById.ContainsKey(id)).ToList();
            if (missingSubgroupIds.Count > 0)
            {
                var subgroupEntities = await DbContext.ItemGroups
                    .AsNoTracking()
                    .Where(g => missingSubgroupIds.Contains(g.Id))
                    .ToListAsync(cancellationToken);
                foreach (var group in subgroupEntities)
                {
                    groupsById[group.Id] = group;
                }
            }
        }

        var groupNameById = groupsById.ToDictionary(kv => kv.Key, kv => kv.Value.Name ?? string.Empty);

        var unitExternalIds = components
            .Where(x => x.UnitId.HasValue)
            .Select(x => x.UnitId!.Value.ToString(CultureInfo.InvariantCulture))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var unitOfMeasureIdByExternalId = unitExternalIds.Count > 0
            ? (await DbContext.ExternalEntityLinks
                    .AsNoTracking()
                    .Where(l =>
                        l.EntityType == nameof(UnitOfMeasure)
                        && l.ExternalSystem == ComponentExternalSystem
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

        var productLinks = await DbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l =>
                l.EntityType == nameof(Item)
                && l.ExternalSystem == ProductExternalSystem
                && l.ExternalEntity == ProductExternalEntity)
            .ToListAsync(cancellationToken);

        var productItemsByExternalId = productLinks
            .GroupBy(l => l.ExternalId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().EntityId, StringComparer.Ordinal);

        var productItemIds = productItemsByExternalId.Values.Distinct().ToList();
        var productItemsById = productItemIds.Count == 0
            ? new Dictionary<Guid, Item>()
            : await DbContext.Items
                .AsNoTracking()
                .Where(i => productItemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

        var productItemsByDesignation = productItemsById.Values
            .Where(x => !string.IsNullOrWhiteSpace(x.Designation))
            .GroupBy(x => NormalizeDesignationKey(x.Designation))
            .ToDictionary(g => g.Key!, g => g.ToList(), StringComparer.Ordinal);

        var componentLinks = await DbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l =>
                l.EntityType == nameof(Item)
                && l.ExternalSystem == ComponentExternalSystem
                && l.ExternalEntity == ComponentExternalEntity)
            .ToListAsync(cancellationToken);

        var componentItemsByExternalId = componentLinks
            .GroupBy(l => l.ExternalId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().EntityId, StringComparer.Ordinal);

        var componentItemIds = componentItemsByExternalId.Values.Distinct().ToList();
        var componentItemsById = componentItemIds.Count == 0
            ? new Dictionary<Guid, Item>()
            : await DbContext.Items
                .AsNoTracking()
                .Where(i => componentItemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken);

        var incomingCodes = components
            .Select(i => NormalizeOptional(i.Code))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existingItemsByCode = incomingCodes.Count == 0
            ? new Dictionary<string, Item>(StringComparer.Ordinal)
            : await DbContext.Items
                .AsNoTracking()
                .Where(i => incomingCodes.Contains(i.Code))
                .ToDictionaryAsync(i => i.Code, StringComparer.Ordinal, cancellationToken);

        var previewItems = new List<Component2020ImportPreviewItemDto>(products.Count + components.Count);

        var productDesignationKeys = products
            .Select(ResolveProductDesignationKey)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var productCandidatesByDesignation = productDesignationKeys.Count > 0
            ? (await DbContext.Items
                    .AsNoTracking()
                    .Where(i => i.Designation != null && productDesignationKeys.Contains(i.Designation.ToUpper()))
                    .ToListAsync(cancellationToken))
                .GroupBy(i => i.Designation!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, List<Item>>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            var reasons = new List<string>();
            var itemKind = ResolveProductItemKind(product, reasons);
            var productName = string.IsNullOrWhiteSpace(product.Description) ? product.Name : product.Description ?? product.Name;
            Guid? assemblySubgroupId = null;
            if (itemKind == ItemKind.Assembly)
            {
                var subgroup = ResolveAssemblySubgroup(productName);
                assemblySubgroupId = ResolveAssemblySubgroupId(productName, assemblySubgroupIds);
                if (assemblySubgroupId.HasValue && subgroup.HasValue && groupNameById.TryGetValue(assemblySubgroupId.Value, out var subgroupName))
                {
                    var keyword = subgroup.Value switch
                    {
                        AssemblySubgroup.Modules => "Модуль",
                        AssemblySubgroup.Nodes => productName?.IndexOf("плата", StringComparison.OrdinalIgnoreCase) >= 0 ? "Плата" : "Узел",
                        AssemblySubgroup.Assemblies => "Корпус",
                        _ => "keyword"
                    };
                    reasons.Add($"Assembly '{keyword}' -> group '{subgroupName}'");
                }
            }

            if (product.GroupId.HasValue)
            {
                var groupLabel = externalGroups.TryGetValue(product.GroupId.Value, out var ignoredGroupName)
                    ? ignoredGroupName
                    : product.GroupId.Value.ToString(CultureInfo.InvariantCulture);
                reasons.Add($"Product.GroupId ignored ({groupLabel})");
            }

            var groupResolution = ResolveGroupForPreview(
                null,
                itemKind,
                groupIdByExternalId,
                groupNameById,
                rootAbbreviationByGroupId,
                defaultGroupIds,
                reasons,
                externalGroups);

            if (assemblySubgroupId.HasValue)
            {
                var subgroupName = groupNameById.TryGetValue(assemblySubgroupId.Value, out var name)
                    ? name
                    : null;
                var rootAbbr = rootAbbreviationByGroupId.TryGetValue(assemblySubgroupId.Value, out var root)
                    ? root
                    : groupResolution.RootAbbreviation;
                groupResolution = new GroupResolution(assemblySubgroupId, subgroupName, rootAbbr);
            }

            var externalId = product.Id.ToString(CultureInfo.InvariantCulture);
            var hasExistingItem = productItemsByExternalId.TryGetValue(externalId, out var existingItemId);
            var action = hasExistingItem ? "Update" : "Create";

            Item? existingItem = null;
            if (hasExistingItem && productItemsById.TryGetValue(existingItemId, out var existing))
            {
                existingItem = existing;
            }
            else
            {
                var designationKey = ResolveProductDesignationKey(product);
                if (!string.IsNullOrWhiteSpace(designationKey)
                    && productCandidatesByDesignation.TryGetValue(designationKey, out var candidates))
                {
                    if (candidates.Count == 1)
                    {
                        existingItem = candidates[0];
                        action = "Update";
                        reasons.Add($"Matched by designation to Item {existingItem.Id}");
                    }
                    else if (candidates.Count > 1)
                    {
                        action = "Review";
                        reasons.Add($"Multiple Item matches by designation ({candidates.Count})");
                    }
                }
            }

            previewItems.Add(new Component2020ImportPreviewItemDto
            {
                Source = "Product",
                ExternalId = product.Id,
                ExternalGroupId = product.GroupId?.ToString(CultureInfo.InvariantCulture),
                ExternalGroupName = product.GroupId.HasValue && externalGroups.TryGetValue(product.GroupId.Value, out var externalGroupName)
                    ? externalGroupName
                    : null,
                Code = null,
                PartNumber = null,
                Designation = product.Name,
                DesignationSource = "Product.Name",
                DesignationCandidates = Array.Empty<string>(),
                Name = productName,
                Description = product.Description,
                UnitName = null,
                UnitSymbol = null,
                ItemKind = itemKind.ToString(),
                ItemGroupId = groupResolution.GroupId?.ToString(),
                ItemGroupName = groupResolution.GroupName,
                RootGroupAbbreviation = groupResolution.RootAbbreviation,
                Action = action,
                Reasons = reasons.ToArray(),
                ExistingItemId = existingItem?.Id.ToString(),
                ExistingItemKind = existingItem?.ItemKind.ToString(),
                ExistingItemGroup = existingItem?.ItemGroupId.HasValue == true && groupNameById.TryGetValue(existingItem.ItemGroupId.Value, out var existingGroupName)
                    ? existingGroupName
                    : null,
                MatchedItemId = null,
                MatchedItemKind = null,
                MatchedItemGroup = null,
                IsTooling = false
            });
        }

        foreach (var component in components)
        {
            var reasons = new List<string>();
            var groupName = component.GroupId.HasValue && externalGroups.TryGetValue(component.GroupId.Value, out var groupLabel)
                ? groupLabel
                : null;

            UnitOfMeasure? unit = null;
            if (component.UnitId.HasValue
                && unitOfMeasureIdByExternalId.TryGetValue(component.UnitId.Value.ToString(CultureInfo.InvariantCulture), out var mappedUnitId))
            {
                unitsById.TryGetValue(mappedUnitId, out unit);
            }

            var designationResult = ExtractDesignationCandidates(component);
            var designationCandidates = designationResult.candidates;
            var designationSource = designationResult.source;
            string? componentDesignation = null;
            if (designationCandidates.Count == 1)
            {
                componentDesignation = designationCandidates[0];
            }
            else if (designationCandidates.Count > 1)
            {
                reasons.Add("Multiple designation candidates found");
            }

            var itemKind = ResolveComponentItemKind(component, groupName, unit, reasons, componentDesignation);

            var resolvedName = ResolveComponentName(component.Name, componentDesignation);
            if (!string.Equals(resolvedName, component.Name?.Trim() ?? string.Empty, StringComparison.Ordinal))
            {
                reasons.Add("Name trimmed by designation prefix");
            }

            var groupResolution = ResolveGroupForPreview(
                component.GroupId,
                itemKind,
                groupIdByExternalId,
                groupNameById,
                rootAbbreviationByGroupId,
                defaultGroupIds,
                reasons,
                externalGroups);

            if (!string.IsNullOrWhiteSpace(componentDesignation)
                && component.GroupId.HasValue
                && groupIdByExternalId.TryGetValue(component.GroupId.Value.ToString(CultureInfo.InvariantCulture), out var mappedGroupId)
                && groupResolution.GroupId != mappedGroupId)
            {
                var mappedGroupName = groupNameById.TryGetValue(mappedGroupId, out var mappedName) ? mappedName : null;
                var mappedRootAbbr = rootAbbreviationByGroupId.TryGetValue(mappedGroupId, out var mappedRoot)
                    ? mappedRoot
                    : groupResolution.RootAbbreviation;
                groupResolution = new GroupResolution(mappedGroupId, mappedGroupName, mappedRootAbbr);
                reasons.Add("Designation -> PRT; keep Access group");
            }

            var externalId = component.Id.ToString(CultureInfo.InvariantCulture);
            Item? existingItem = null;
            if (componentItemsByExternalId.TryGetValue(externalId, out var componentItemId))
            {
                componentItemsById.TryGetValue(componentItemId, out existingItem);
            }

            var action = existingItem != null ? "Update" : "Create";
            Item? matchedItem = null;

            var designationKey = NormalizeDesignationKey(componentDesignation);
                if (existingItem == null && designationKey != null && productItemsByDesignation.TryGetValue(designationKey, out var candidates))
                {
                    if (candidates.Count == 1)
                    {
                        matchedItem = candidates[0];
                        if (matchedItem.ItemKind != itemKind)
                        {
                            action = "Merge";
                            reasons.Add($"Classification conflict resolved by Product priority: Component={itemKind}, Product={matchedItem.ItemKind}");
                        }
                        else
                        {
                            action = "Merge";
                            reasons.Add($"Match by designation to Product {matchedItem.Id}");
                        }
                }
                else if (candidates.Count > 1)
                {
                    action = "Review";
                    reasons.Add($"Multiple Product matches by designation ({candidates.Count})");
                }
            }

            if (existingItem == null && matchedItem == null)
            {
                var itemCode = NormalizeOptional(component.Code);
                if (!string.IsNullOrWhiteSpace(itemCode) && existingItemsByCode.TryGetValue(itemCode, out var existingByCode))
                {
                    existingItem = existingByCode;
                    action = "Update";
                    reasons.Add($"Matched existing item by code {itemCode}");
                }
            }

            previewItems.Add(new Component2020ImportPreviewItemDto
            {
                Source = "Component",
                ExternalId = component.Id,
                ExternalGroupId = component.GroupId?.ToString(CultureInfo.InvariantCulture),
                ExternalGroupName = component.GroupId.HasValue && externalGroups.TryGetValue(component.GroupId.Value, out var externalGroupName)
                    ? externalGroupName
                    : null,
                Code = NormalizeOptional(component.Code),
                PartNumber = component.PartNumber,
                Designation = componentDesignation,
                DesignationSource = designationSource,
                DesignationCandidates = designationCandidates.ToArray(),
                Name = resolvedName,
                Description = component.Description,
                UnitName = unit?.Name,
                UnitSymbol = unit?.Symbol,
                ItemKind = itemKind.ToString(),
                ItemGroupId = groupResolution.GroupId?.ToString(),
                ItemGroupName = groupResolution.GroupName,
                RootGroupAbbreviation = groupResolution.RootAbbreviation,
                Action = action,
                Reasons = reasons.ToArray(),
                ExistingItemId = existingItem?.Id.ToString(),
                ExistingItemKind = existingItem?.ItemKind.ToString(),
                ExistingItemGroup = existingItem?.ItemGroupId.HasValue == true && groupNameById.TryGetValue(existingItem.ItemGroupId.Value, out var existingGroupName)
                    ? existingGroupName
                    : null,
                MatchedItemId = matchedItem?.Id.ToString(),
                MatchedItemKind = matchedItem?.ItemKind.ToString(),
                MatchedItemGroup = matchedItem?.ItemGroupId.HasValue == true && groupNameById.TryGetValue(matchedItem.ItemGroupId.Value, out var matchedGroupName)
                    ? matchedGroupName
                    : null,
                IsTooling = component.CanMeans == true
            });
        }

        var summary = new Component2020ImportPreviewSummaryDto
        {
            Total = previewItems.Count,
            Products = previewItems.Count(x => x.Source == "Product"),
            Components = previewItems.Count(x => x.Source == "Component"),
            Create = previewItems.Count(x => x.Action == "Create"),
            Update = previewItems.Count(x => x.Action == "Update"),
            Merge = previewItems.Count(x => x.Action == "Merge"),
            Review = previewItems.Count(x => x.Action == "Review")
        };

        var total = previewItems.Count;
        var skip = (request.Page - 1) * request.PageSize;
        var pageItems = previewItems
            .Skip(skip)
            .Take(request.PageSize)
            .ToArray();

        return new Component2020ImportPreviewResponseDto
        {
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize,
            Summary = summary,
            Items = pageItems
        };
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
            var remainder = name.Substring(trimmedDesignation.Length).TrimStart();
            if (!string.IsNullOrWhiteSpace(remainder))
            {
                return remainder;
            }
        }

        return name;
    }

    private static (IReadOnlyList<string> candidates, string? source) ExtractDesignationCandidates(Component2020Item item)
    {
        var fromPartNumber = ExtractDesignationCandidates(item.PartNumber);
        if (fromPartNumber.Count > 0)
        {
            return (fromPartNumber, "PartNumber");
        }

        var fromName = ExtractDesignationCandidates(item.Name);
        if (fromName.Count > 0)
        {
            return (fromName, "Name");
        }

        var fromDescription = ExtractDesignationCandidates(item.Description);
        if (fromDescription.Count > 0)
        {
            return (fromDescription, "Description");
        }

        return (Array.Empty<string>(), null);
    }

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

    private static ItemKind ResolveComponentItemKind(Component2020Item item, string? groupName, UnitOfMeasure? unit, List<string> reasons, string? designation)
    {
        if (!string.IsNullOrWhiteSpace(designation))
        {
            reasons.Add("Designation -> PRT");
            return ItemKind.ManufacturedPart;
        }

        if (item.BomSection == 3)
        {
            reasons.Add("BOMSection=3 -> MAT");
            return ItemKind.Material;
        }

        if (item.BomSection == 1)
        {
            reasons.Add("BOMSection=1 -> STD");
            return ItemKind.StandardPart;
        }

        if (ContainsAny(groupName, ServiceKeywords))
        {
            reasons.Add("Group contains service keywords -> SRV");
            return ItemKind.ServiceWork;
        }

        if (ContainsAny(groupName, MaterialKeywords) || IsMaterialUnit(unit))
        {
            reasons.Add("Group/material unit -> MAT");
            return ItemKind.Material;
        }

        if (ContainsAny(groupName, StandardPartKeywords)
            || HasGostOrTu(item.Name)
            || HasGostOrTu(item.Description)
            || HasGostOrTu(item.PartNumber))
        {
            reasons.Add("Group/ГОСТ/ТУ -> STD");
            return ItemKind.StandardPart;
        }

        if (ContainsAny(groupName, ManufacturedPartKeywords))
        {
            reasons.Add("Group contains manufactured keywords -> PRT");
            return ItemKind.ManufacturedPart;
        }

        if (item.ManufacturerId.HasValue
            || !string.IsNullOrWhiteSpace(item.PartNumber)
            || !string.IsNullOrWhiteSpace(item.DataSheet)
            || ContainsAny(groupName, PurchasedComponentKeywords))
        {
            reasons.Add("Manufacturer/PartNumber/DataSheet/keywords -> CMP");
            return ItemKind.PurchasedComponent;
        }

        reasons.Add("Default -> CMP");
        return ItemKind.PurchasedComponent;
    }

    private static ItemKind ResolveProductItemKind(Component2020Product product, List<string> reasons)
    {
        if (product.Kind == 1)
        {
            reasons.Add("Kind=1 -> PRT");
            return ItemKind.ManufacturedPart;
        }

        reasons.Add("Default -> ASM");
        return ItemKind.Assembly;
    }

    private static string? NormalizeDesignationKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? ResolveProductDesignationKey(Component2020Product product)
    {
        var candidates = ExtractDesignationCandidates(product.Name);
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        candidates = ExtractDesignationCandidates(product.Description);
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        return product.Name?.Trim();
    }

    private static GroupResolution ResolveGroupForPreview(
        int? externalGroupId,
        ItemKind itemKind,
        Dictionary<string, Guid> groupIdByExternalId,
        Dictionary<Guid, string> groupNameById,
        Dictionary<Guid, string?> rootAbbreviationByGroupId,
        Dictionary<ItemKind, Guid?> defaultGroupIds,
        List<string> reasons,
        Dictionary<int, string> externalGroups)
    {
        Guid? mappedGroupId = null;
        if (externalGroupId.HasValue
            && groupIdByExternalId.TryGetValue(externalGroupId.Value.ToString(CultureInfo.InvariantCulture), out var internalGroupId))
        {
            mappedGroupId = internalGroupId;
            if (externalGroups.TryGetValue(externalGroupId.Value, out var groupName))
            {
                reasons.Add($"Mapped group '{groupName}' ({externalGroupId})");
            }
            else
            {
                reasons.Add($"Mapped group {externalGroupId}");
            }
        }
        else if (externalGroupId.HasValue)
        {
            reasons.Add($"Group {externalGroupId} not mapped");
        }
        else
        {
            reasons.Add("No external group");
        }

        var resolvedGroupId = ResolveItemGroupIdForKind(mappedGroupId, itemKind, rootAbbreviationByGroupId, defaultGroupIds);
        var resolvedGroupName = resolvedGroupId.HasValue && groupNameById.TryGetValue(resolvedGroupId.Value, out var name)
            ? name
            : null;

        var rootAbbr = resolvedGroupId.HasValue && rootAbbreviationByGroupId.TryGetValue(resolvedGroupId.Value, out var root)
            ? root
            : null;

        if (resolvedGroupId.HasValue && mappedGroupId.HasValue && resolvedGroupId.Value != mappedGroupId.Value)
        {
            reasons.Add($"Group root '{rootAbbr ?? "?"}' mismatches ItemKind, fallback to '{resolvedGroupName ?? resolvedGroupId.Value.ToString()}'");
        }
        else if (resolvedGroupId.HasValue && mappedGroupId == null)
        {
            reasons.Add($"Fallback to '{resolvedGroupName ?? resolvedGroupId.Value.ToString()}'");
        }

        return new GroupResolution(resolvedGroupId, resolvedGroupName, rootAbbr);
    }

    private sealed record GroupResolution(Guid? GroupId, string? GroupName, string? RootAbbreviation);
}
