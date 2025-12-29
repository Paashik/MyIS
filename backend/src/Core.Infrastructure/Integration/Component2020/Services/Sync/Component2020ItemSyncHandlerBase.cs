using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.Services;
using MyIS.Core.Domain.Mdm.ValueObjects;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public abstract class Component2020ItemSyncHandlerBase
{
    protected Component2020ItemSyncHandlerBase(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        SnapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        ExternalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected AppDbContext DbContext { get; }
    protected IComponent2020SnapshotReader SnapshotReader { get; }
    protected Component2020ExternalLinkHelper ExternalLinkHelper { get; }
    protected ILogger Logger { get; }

    protected sealed class DryRunSequenceState
    {
        public required string Prefix { get; init; }
        public int NextNumber { get; set; }
    }

    protected sealed record ItemGroupMappings(
        Dictionary<int, Guid> ItemGroupIdByExternalId,
        Dictionary<int, int> RootGroupIdByExternalId,
        Dictionary<int, string?> RootAbbreviationByExternalId,
        Dictionary<int, string> GroupNameByExternalId);

    protected static readonly IReadOnlyDictionary<int, string> DefaultRootGroupAbbreviationById =
        new Dictionary<int, string>
        {
            // Root Groups (Access.Groups.Parent = 0)
            [221] = "CMP", // Покупные комплектующие
            [224] = "MAT", // Сырье и материалы
            [225] = "PRD", // Готовая продукция
            [226] = "SRV", // Работы и услуги
            [184] = "SFG"  // Полуфабрикаты
        };

    protected static readonly HashSet<int> RootGroupsWithoutAbbreviation = new()
    {
        183, // УДАЛЕННОЕ
        196  // NO BOM
    };

    protected static string ResolveNomenclaturePrefix(
        ItemKind itemKind,
        int? groupId,
        Dictionary<int, int> rootGroupIdByExternalId,
        Dictionary<int, string?> rootAbbreviationByExternalId)
    {
        if (!groupId.HasValue)
        {
            return ItemNomenclature.GetDefaultPrefix(itemKind);
        }

        if (!rootGroupIdByExternalId.TryGetValue(groupId.Value, out var rootGroupId))
        {
            return ItemNomenclature.GetDefaultPrefix(itemKind);
        }

        if (rootAbbreviationByExternalId.TryGetValue(rootGroupId, out var abbreviation)
            && !string.IsNullOrWhiteSpace(abbreviation))
        {
            return abbreviation.Trim().ToUpperInvariant();
        }

        return ItemNomenclature.GetDefaultPrefix(itemKind);
    }

    protected static bool IsValidNomenclatureNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var v = value.Trim();
        if (v.Length != 10 || v[3] != '-')
        {
            return false;
        }

        for (var i = 0; i < 3; i++)
        {
            var c = v[i];
            if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9'))
            {
                return false;
            }
        }

        for (var i = 4; i < 10; i++)
        {
            if (v[i] < '0' || v[i] > '9')
            {
                return false;
            }
        }

        return true;
    }

    protected async Task<int> GetMaxUsedNomenclatureNumberAsync(string prefix, CancellationToken cancellationToken)
    {
        var values = await DbContext.Items
            .AsNoTracking()
            .Where(i => i.NomenclatureNo.StartsWith($"{prefix}-"))
            .Select(i => i.NomenclatureNo)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var nomenclatureNo in values)
        {
            if (ItemNomenclature.TryExtractNumericSuffix(nomenclatureNo, prefix, out var current) && current > max)
            {
                max = current;
            }
        }

        return max;
    }

    protected async Task<ItemSequence?> FindItemSequenceAsync(ItemKind itemKind, bool forUpdate, CancellationToken cancellationToken)
    {
        var providerName = DbContext.Database.ProviderName;
        var canLock = forUpdate
                      && providerName == "Npgsql.EntityFrameworkCore.PostgreSQL"
                      && DbContext.Database.CurrentTransaction != null;

        if (canLock)
        {
            return await DbContext.ItemSequences
                .FromSqlInterpolated($"SELECT * FROM mdm.item_sequences WHERE \"ItemKind\" = {(int)itemKind} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await DbContext.ItemSequences.SingleOrDefaultAsync(s => s.ItemKind == itemKind, cancellationToken);
    }

    protected async Task<ItemSequence> GetOrCreateSequenceAsync(
        ItemKind itemKind,
        string prefix,
        Dictionary<ItemKind, ItemSequence> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(itemKind, out var cached))
        {
            if (!string.Equals(cached.Prefix, prefix, StringComparison.Ordinal))
            {
                var cachedMaxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                cached.SetPrefixAndNextNumber(prefix, cachedMaxUsed + 1);
            }

            return cached;
        }

        var existing = await FindItemSequenceAsync(itemKind, forUpdate: true, cancellationToken);
        if (existing != null)
        {
            if (!string.Equals(existing.Prefix, prefix, StringComparison.Ordinal))
            {
                var existingMaxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                existing.SetPrefixAndNextNumber(prefix, existingMaxUsed + 1);
            }

            cache[itemKind] = existing;
            return existing;
        }

        var maxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
        var created = new ItemSequence(itemKind, prefix, maxUsed + 1);
        DbContext.ItemSequences.Add(created);
        cache[itemKind] = created;
        return created;
    }

    protected async Task<string> GenerateNextNomenclatureNoAsync(
        ItemKind itemKind,
        string prefix,
        bool dryRun,
        Dictionary<ItemKind, ItemSequence> sequences,
        Dictionary<ItemKind, DryRunSequenceState> dryRunSequences,
        CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            if (!dryRunSequences.TryGetValue(itemKind, out var state) || !string.Equals(state.Prefix, prefix, StringComparison.Ordinal))
            {
                var existing = await FindItemSequenceAsync(itemKind, forUpdate: false, cancellationToken);
                if (existing != null && string.Equals(existing.Prefix, prefix, StringComparison.Ordinal))
                {
                    state = new DryRunSequenceState { Prefix = prefix, NextNumber = existing.NextNumber };
                }
                else
                {
                    var max = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                    state = new DryRunSequenceState { Prefix = prefix, NextNumber = max + 1 };
                }

                dryRunSequences[itemKind] = state;
            }

            var number = state.NextNumber;
            state.NextNumber++;
            return ItemNomenclature.FormatNomenclatureNo(prefix, number);
        }

        var sequence = await GetOrCreateSequenceAsync(itemKind, prefix, sequences, cancellationToken);
        var next = sequence.NextNumber;
        sequence.IncrementNextNumber();
        return ItemNomenclature.FormatNomenclatureNo(prefix, next);
    }

    protected static int ResolveRootGroupId(int id, Dictionary<int, int?> parentById)
    {
        var visited = new HashSet<int>();
        var current = id;
        const int maxDepth = 100; // Prevent infinite loops
        var depth = 0;

        while (depth < maxDepth)
        {
            if (!visited.Add(current))
            {
                // Circular reference detected, return current as root
                return current;
            }

            if (!parentById.TryGetValue(current, out var parentId) || parentId == null || parentId.Value <= 0)
            {
                return current;
            }

            current = parentId.Value;
            depth++;
        }

        // Max depth reached, return current as root
        return current;
    }

    protected static ItemKind MapRootGroupToItemKind(int rootGroupId) =>
        rootGroupId switch
        {
            221 => ItemKind.PurchasedComponent, // Покупные комплектующие
            224 => ItemKind.Material,  // Сырье и материалы
            225 => ItemKind.Product,   // Готовая продукция
            226 => ItemKind.ServiceWork,   // Работы и услуги
            184 => ItemKind.Assembly,  // Полуфабрикаты
            _ => ItemKind.PurchasedComponent
        };

    protected static ItemKind ResolveItemKindByGroupRoot(int? groupId, Dictionary<int, int> rootGroupIdByExternalId, ItemKind fallback)
    {
        if (!groupId.HasValue)
        {
            return fallback;
        }

        if (rootGroupIdByExternalId.TryGetValue(groupId.Value, out var rootGroupId))
        {
            return MapRootGroupToItemKind(rootGroupId);
        }

        return fallback;
    }

    protected static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static string? ResolveExpectedRootAbbreviation(ItemKind itemKind) =>
        itemKind switch
        {
            ItemKind.PurchasedComponent => "CMP",
            ItemKind.Material => "MAT",
            ItemKind.Product => "PRD",
            ItemKind.ServiceWork => "SRV",
            ItemKind.Assembly => "ASM",
            ItemKind.ManufacturedPart => "PRT",
            ItemKind.StandardPart => "CMP",
            _ => null
        };

    protected enum AssemblySubgroup
    {
        Modules,
        Nodes,
        Assemblies
    }

    protected static AssemblySubgroup? ResolveAssemblySubgroup(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (name.IndexOf("модул", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return AssemblySubgroup.Modules;
        }

        if (name.IndexOf("узл", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return AssemblySubgroup.Nodes;
        }

        if (name.IndexOf("плата", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return AssemblySubgroup.Nodes;
        }

        if (name.IndexOf("корпус", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return AssemblySubgroup.Assemblies;
        }

        return null;
    }

    protected static Guid? ResolveAssemblySubgroupId(string? name, Dictionary<AssemblySubgroup, Guid> subgroupIds)
    {
        var subgroup = ResolveAssemblySubgroup(name);
        if (subgroup.HasValue && subgroupIds.TryGetValue(subgroup.Value, out var id))
        {
            return id;
        }

        return null;
    }

    protected async Task<Dictionary<AssemblySubgroup, Guid>> LoadAssemblySubgroupIdsAsync(
        Dictionary<ItemKind, Guid?> defaultGroupIds,
        CancellationToken cancellationToken)
    {
        if (!defaultGroupIds.TryGetValue(ItemKind.Assembly, out var asmRootId) || !asmRootId.HasValue)
        {
            return new Dictionary<AssemblySubgroup, Guid>();
        }

        var groups = await DbContext.ItemGroups
            .AsNoTracking()
            .Select(g => new { g.Id, g.ParentId, g.Name })
            .ToListAsync(cancellationToken);

        var childrenByParent = groups
            .Where(g => g.ParentId.HasValue)
            .GroupBy(g => g.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var queue = new Queue<(Guid id, int depth)>();
        queue.Enqueue((asmRootId.Value, 0));

        var visited = new HashSet<Guid>();
        var descendants = new List<(Guid id, int depth, string name)>();

        while (queue.Count > 0)
        {
            var (currentId, depth) = queue.Dequeue();
            if (!visited.Add(currentId))
            {
                continue;
            }

            if (childrenByParent.TryGetValue(currentId, out var children))
            {
                foreach (var child in children)
                {
                    var childName = child.Name ?? string.Empty;
                    descendants.Add((child.Id, depth + 1, childName));
                    queue.Enqueue((child.Id, depth + 1));
                }
            }
        }

        var result = new Dictionary<AssemblySubgroup, Guid>();
        foreach (var group in descendants.OrderBy(g => g.depth).ThenBy(g => g.name))
        {
            var name = group.name ?? string.Empty;
            if (!result.ContainsKey(AssemblySubgroup.Modules)
                && name.IndexOf("модул", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result[AssemblySubgroup.Modules] = group.id;
                continue;
            }

            if (!result.ContainsKey(AssemblySubgroup.Nodes)
                && name.IndexOf("узл", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result[AssemblySubgroup.Nodes] = group.id;
                continue;
            }

            if (!result.ContainsKey(AssemblySubgroup.Assemblies)
                && name.IndexOf("сборк", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result[AssemblySubgroup.Assemblies] = group.id;
            }
        }

        return result;
    }

    protected static Guid? ResolveItemGroupIdForKind(
        Guid? itemGroupId,
        ItemKind itemKind,
        Dictionary<Guid, string?> rootAbbreviationByGroupId,
        Dictionary<ItemKind, Guid?> defaultGroupIds)
    {
        if (!itemGroupId.HasValue)
        {
            return defaultGroupIds.TryGetValue(itemKind, out var fallback) ? fallback : null;
        }

        var expectedRoot = ResolveExpectedRootAbbreviation(itemKind);
        if (string.IsNullOrWhiteSpace(expectedRoot))
        {
            return itemGroupId;
        }

        if (!rootAbbreviationByGroupId.TryGetValue(itemGroupId.Value, out var rootAbbreviation)
            || string.IsNullOrWhiteSpace(rootAbbreviation))
        {
            return itemGroupId;
        }

        if (!string.Equals(rootAbbreviation, expectedRoot, StringComparison.OrdinalIgnoreCase)
            && defaultGroupIds.TryGetValue(itemKind, out var fallbackId)
            && fallbackId.HasValue)
        {
            return fallbackId.Value;
        }

        return itemGroupId;
    }

    protected async Task<Dictionary<ItemKind, Guid?>> LoadDefaultItemGroupIdsAsync(CancellationToken cancellationToken)
    {
        var abbreviations = new[] { "CMP", "MAT", "PRD", "SRV", "ASM", "PRT", "STD" };
        var groups = await DbContext.ItemGroups
            .AsNoTracking()
            .Where(g => g.Abbreviation != null && abbreviations.Contains(g.Abbreviation))
            .ToListAsync(cancellationToken);

        Guid? ResolveByAbbreviation(string abbreviation, bool requireRoot)
        {
            var matches = groups
                .Where(g => string.Equals(g.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase));

            if (requireRoot)
            {
                var root = matches.FirstOrDefault(g => g.ParentId == null);
                if (root != null)
                {
                    return root.Id;
                }
            }

            return matches.FirstOrDefault()?.Id;
        }

        return new Dictionary<ItemKind, Guid?>
        {
            [ItemKind.PurchasedComponent] = ResolveByAbbreviation("CMP", requireRoot: true),
            [ItemKind.Material] = ResolveByAbbreviation("MAT", requireRoot: true),
            [ItemKind.Product] = ResolveByAbbreviation("PRD", requireRoot: true),
            [ItemKind.ServiceWork] = ResolveByAbbreviation("SRV", requireRoot: true),
            [ItemKind.Assembly] = ResolveByAbbreviation("ASM", requireRoot: true),
            [ItemKind.ManufacturedPart] = ResolveByAbbreviation("PRT", requireRoot: true),
            [ItemKind.StandardPart] = ResolveByAbbreviation("STD", requireRoot: false)
        };
    }

    protected async Task<Dictionary<Guid, string?>> LoadGroupRootAbbreviationsAsync(CancellationToken cancellationToken)
    {
        var groups = await DbContext.ItemGroups
            .AsNoTracking()
            .Select(g => new { g.Id, g.ParentId, g.Abbreviation })
            .ToListAsync(cancellationToken);

        var byId = groups.ToDictionary(g => g.Id);
        var cache = new Dictionary<Guid, string?>();

        string? ResolveRootAbbreviation(Guid id)
        {
            if (cache.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var visited = new HashSet<Guid>();
            var current = id;
            while (true)
            {
                if (!visited.Add(current))
                {
                    cache[id] = null;
                    return null;
                }

                if (!byId.TryGetValue(current, out var entry))
                {
                    cache[id] = null;
                    return null;
                }

                if (!entry.ParentId.HasValue)
                {
                    cache[id] = entry.Abbreviation;
                    return entry.Abbreviation;
                }

                current = entry.ParentId.Value;
            }
        }

        foreach (var group in byId.Keys)
        {
            _ = ResolveRootAbbreviation(group);
        }

        return cache;
    }

    protected async Task<ItemGroupMappings> EnsureItemGroupsAsync(Guid connectionId, bool dryRun, CancellationToken cancellationToken)
    {
        const string externalSystem = "Component2020";
        const string externalEntity = "Groups";
        const string linkEntityType = nameof(ItemGroup);

        Logger.LogInformation("Starting Groups import from Component2020 (connectionId={ConnectionId}, dryRun={DryRun})", connectionId, dryRun);

        var groups = (await SnapshotReader.ReadItemGroupsAsync(cancellationToken, connectionId)).ToList();
        Logger.LogInformation("Read {Count} groups from Component2020", groups.Count);

        if (groups.Count == 0)
        {
            Logger.LogWarning("No groups found in Component2020 - returning empty mappings");
            return new ItemGroupMappings(
                new Dictionary<int, Guid>(),
                new Dictionary<int, int>(),
                new Dictionary<int, string?>(),
                new Dictionary<int, string>());
        }

        // Check for duplicates
        var duplicateIds = groups.GroupBy(g => g.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Any())
        {
            Logger.LogWarning("Found duplicate group IDs in Component2020 data: {DuplicateIds}", string.Join(", ", duplicateIds));
            // Remove duplicates, keeping the first occurrence
            groups = groups.GroupBy(g => g.Id).Select(g => g.First()).ToList();
            Logger.LogInformation("Removed duplicates, now have {Count} unique groups", groups.Count);
        }

        var externalIds = groups.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();
        Logger.LogDebug("Processing external IDs: {ExternalIds}", string.Join(", ", externalIds));

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, ItemGroup> existingGroupsById;

        if (!dryRun)
        {
            Logger.LogInformation("Loading existing external links for {Count} groups", externalIds.Count);
            var existingLinks = await DbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            Logger.LogInformation("Found {Count} existing external links", existingLinks.Count);

            // Handle potential duplicates by taking the most recent link
            existingLinksByExternalId = existingLinks
                .GroupBy(l => l.ExternalId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.SyncedAt).First(), StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            Logger.LogInformation("Loading existing ItemGroups for {Count} IDs", ids.Count);
            var existingEntities = await DbContext.ItemGroups
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            Logger.LogInformation("Found {Count} existing ItemGroups", existingEntities.Count);
            existingGroupsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            Logger.LogInformation("Running in dry mode - skipping database lookups");
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingGroupsById = new Dictionary<Guid, ItemGroup>();
        }

        var groupsByExternalId = new Dictionary<string, ItemGroup>(StringComparer.Ordinal);
        var createdCount = 0;
        var updatedCount = 0;

        foreach (var group in groups)
        {
            try
            {
                var externalId = group.Id.ToString();
                Logger.LogDebug("Processing group {GroupId} - {GroupName}", group.Id, group.Name);

                ItemGroup? existing = null;

                if (!dryRun && existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    Logger.LogDebug("Found existing link for group {GroupId}", group.Id);
                    existingGroupsById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (dryRun)
                    {
                        Logger.LogDebug("Dry run - would create group {GroupId}", group.Id);
                        continue;
                    }

                    Logger.LogInformation("Creating new ItemGroup: {GroupName} (externalId={ExternalId})", group.Name, externalId);
                    var created = new ItemGroup(group.Name, null, group.Description);
                    DbContext.ItemGroups.Add(created);

                    var now = DateTimeOffset.UtcNow;
                    // If there is an existing link but no corresponding entity, update the link to point to the new entity
                    if (existingLinksByExternalId.TryGetValue(externalId, out var orphanLink))
                    {
                        orphanLink.UpdateEntityId(created.Id, now);
                        Logger.LogDebug("Updated existing orphan link for group {GroupId} to point to new entity", group.Id);
                    }
                    else
                    {
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    groupsByExternalId[externalId] = created;
                    createdCount++;
                    Logger.LogDebug("Successfully created group {GroupId}", group.Id);
                }
                else
                {
                    Logger.LogInformation("Updating existing ItemGroup: {GroupName} (externalId={ExternalId})", group.Name, externalId);
                    if (!dryRun)
                    {
                        existing.Update(group.Name, existing.ParentId, group.Description);
                        var now = DateTimeOffset.UtcNow;
                        ExternalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    groupsByExternalId[externalId] = existing;
                    updatedCount++;
                    Logger.LogDebug("Successfully updated group {GroupId}", group.Id);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing group {GroupId}: {Message}", group.Id, ex.Message);
                throw; // Re-throw to be caught by caller
            }
        }

        Logger.LogInformation("Groups processing completed: created={Created}, updated={Updated}", createdCount, updatedCount);

        if (!dryRun && groupsByExternalId.Count > 0)
        {
            Logger.LogInformation("Saving initial groups to database ({Count} groups to save)", groupsByExternalId.Count);
            await DbContext.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Successfully saved initial groups to database");

            Logger.LogInformation("Processing parent relationships for {Count} groups", groups.Count);
            var changed = false;
            var parentProcessedCount = 0;
            foreach (var group in groups)
            {
                try
                {
                    var externalId = group.Id.ToString();
                    if (!groupsByExternalId.TryGetValue(externalId, out var entity))
                    {
                        Logger.LogDebug("Group {GroupId} not found in processed groups - skipping parent processing", group.Id);
                        continue;
                    }

                    Guid? desiredParentId = null;
                    if (group.ParentId != null && group.ParentId.Value > 0 && group.ParentId.Value != group.Id)
                    {
                        var parentExternalId = group.ParentId.Value.ToString();
                        Logger.LogDebug("Group {GroupId} has parent {ParentId}", group.Id, group.ParentId.Value);

                        if (!groupsByExternalId.TryGetValue(parentExternalId, out var parent))
                        {
                            Logger.LogWarning("Parent group {ParentId} not found in current batch for group {GroupId} - parent relationship will be set later or in subsequent sync", group.ParentId.Value, group.Id);
                            // The parent might exist in a previous sync or future sync
                        }
                        else
                        {
                            Logger.LogDebug("Found parent group {ParentId} for group {GroupId}", parent.Id, group.Id);
                            desiredParentId = parent.Id;
                        }
                    }

                    if (entity.ParentId != desiredParentId)
                    {
                        Logger.LogInformation("Updating parent for group {GroupId}: {OldParentId} -> {NewParentId}",
                            group.Id, entity.ParentId, desiredParentId);
                        entity.Update(entity.Name, desiredParentId);
                        changed = true;
                    }
                    parentProcessedCount++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing parent relationship for group {GroupId}", group.Id);
                    throw;
                }
            }

            Logger.LogInformation("Processed parent relationships for {ProcessedCount} out of {TotalCount} groups", parentProcessedCount, groups.Count);

            if (changed)
            {
                Logger.LogInformation("Saving parent relationship changes to database");
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            Logger.LogInformation("Processing abbreviations for root groups");
            var abbreviationChanged = false;
            foreach (var group in groups)
            {
                var externalId = group.Id.ToString();
                if (!groupsByExternalId.TryGetValue(externalId, out var entity))
                {
                    continue;
                }

                if (group.ParentId == null || group.ParentId.Value <= 0)
                {
                    if (RootGroupsWithoutAbbreviation.Contains(group.Id))
                    {
                        if (!string.IsNullOrWhiteSpace(entity.Abbreviation))
                        {
                            entity.SetAbbreviation(null);
                            abbreviationChanged = true;
                        }
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entity.Abbreviation)
                        && DefaultRootGroupAbbreviationById.TryGetValue(group.Id, out var defaultAbbreviation))
                    {
                        entity.SetAbbreviation(defaultAbbreviation);
                        abbreviationChanged = true;
                    }
                }
            }

            if (abbreviationChanged)
            {
                Logger.LogInformation("Saving abbreviation changes for root groups");
                await DbContext.SaveChangesAsync(cancellationToken);
            }
        }

        var groupsById = groups.ToDictionary(g => g.Id);
        var parentById = groupsById
            .Where(kv => kv.Value.ParentId.HasValue)
            .ToDictionary(kv => kv.Key, kv => kv.Value.ParentId);

        var itemGroupIdByExternalId = groupsByExternalId
            .ToDictionary(kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value.Id);

        var rootGroupIdByExternalId = new Dictionary<int, int>();
        var rootAbbreviationByExternalId = new Dictionary<int, string?>();

        foreach (var group in groups)
        {
            var rootId = ResolveRootGroupId(group.Id, parentById);
            rootGroupIdByExternalId[group.Id] = rootId;

            if (groupsByExternalId.TryGetValue(rootId.ToString(CultureInfo.InvariantCulture), out var rootEntity))
            {
                var abbreviation = rootEntity.Abbreviation;
                if (string.IsNullOrWhiteSpace(abbreviation) && DefaultRootGroupAbbreviationById.TryGetValue(rootId, out var defaultAbbreviation))
                {
                    abbreviation = defaultAbbreviation;
                }

                if (RootGroupsWithoutAbbreviation.Contains(rootId))
                {
                    abbreviation = null;
                }

                rootAbbreviationByExternalId[rootId] = abbreviation;
            }
        }

        var groupNameByExternalId = groups.ToDictionary(g => g.Id, g => g.Name ?? string.Empty);

        return new ItemGroupMappings(
            itemGroupIdByExternalId,
            rootGroupIdByExternalId,
            rootAbbreviationByExternalId,
            groupNameByExternalId);
    }

    protected async Task<UnitOfMeasure> FindDefaultUnitOfMeasureAsync(CancellationToken cancellationToken)
    {
        // Backward-compatibility and common variants:
        // - older seed: "pcs"
        // - RU: "шт." / "шт"
        // - OKЕI for pieces: 796 (may be stored in Name)
        var preferred = new[] { "796", "pcs", "шт.", "шт", "pc", "piece" };

        foreach (var code in preferred)
        {
            var found = await DbContext.UnitOfMeasures
                .FirstOrDefaultAsync(u => u.Code != null && u.Code.ToLower() == code.ToLower(), cancellationToken);
            if (found != null)
            {
                return found;
            }
        }

        var bySymbol = await DbContext.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Symbol.ToLower() == "шт." || u.Symbol.ToLower() == "шт", cancellationToken);
        if (bySymbol != null)
        {
            return bySymbol;
        }

        return await DbContext.UnitOfMeasures.FirstAsync(cancellationToken);
    }
}

