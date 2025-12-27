using System;
using System.Collections.Generic;
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

public sealed class Component2020UnitsSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020UnitsSyncHandler> _logger;

    public Component2020UnitsSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020UnitsSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Units;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "Unit";
        const string sourceEntity = "Units";
        const string externalSystem = "Component2020";
        const string externalEntity = "Unit";
        const string linkEntityType = nameof(UnitOfMeasure);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var units = (await _deltaReader.ReadUnitsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();
        _logger.LogInformation(
            "Component2020 Units read {Count} rows (mode={Mode}, lastKey={LastKey})",
            units.Count,
            syncMode,
            lastKey ?? "<full>");

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, UnitOfMeasure> existingUnitsById;

        if (!dryRun && units.Count > 0)
        {
            var externalIds = units.Select(u => u.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var unitIds = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingUnits = await _dbContext.UnitOfMeasures
                .Where(u => unitIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            existingUnitsById = existingUnits.ToDictionary(u => u.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingUnitsById = new Dictionary<Guid, UnitOfMeasure>();
        }

        foreach (var unit in units)
        {
            try
            {
                var (code, name, symbol) = MapUnit(unit);
                var externalId = unit.Id.ToString();
                incomingExternalIds?.Add(externalId);

                UnitOfMeasure? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingUnitsById.TryGetValue(existingLink.EntityId, out existing);
                }
                else if (!dryRun)
                {
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        existing = await _dbContext.UnitOfMeasures
                            .FirstOrDefaultAsync(u => u.Code != null && u.Code == code, cancellationToken);
                    }

                    if (existing == null)
                    {
                        existing = await _dbContext.UnitOfMeasures
                            .FirstOrDefaultAsync(u => u.Name == name, cancellationToken);
                    }

                    if (existing != null && !existingUnitsById.ContainsKey(existing.Id))
                    {
                        existingUnitsById[existing.Id] = existing;
                    }
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var created = new UnitOfMeasure(code, name, symbol);
                        var now = DateTimeOffset.UtcNow;
                        _dbContext.UnitOfMeasures.Add(created);
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.Update(code, name, symbol, true);
                        var now = DateTimeOffset.UtcNow;
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), unit.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing unit {UnitId}", unit.Id);
                var error = new Component2020SyncError(runId, entityType, null, unit.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            if (incomingExternalIds.Count == 0)
            {
                errors.Add(new Component2020SyncError(
                    runId,
                    entityType,
                    null,
                    null,
                    "Overwrite requested, but source returned 0 rows. Deletion is skipped to prevent accidental mass delete.",
                    null));
                counters["UnitDeleted"] = 0;
            }
            else
            {
                var deleted = await DeleteMissingUnitsAsync(incomingExternalIds, runId, errors, cancellationToken);
                counters["UnitDeleted"] = deleted;
            }
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private static (string? code, string name, string symbol) MapUnit(Component2020Unit unit)
    {
        var symbol = (unit.Symbol ?? string.Empty).Trim();
        var name = (unit.Name ?? string.Empty).Trim();
        var code = NormalizeOptional(unit.Code);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = !string.IsNullOrWhiteSpace(symbol) ? symbol : unit.Id.ToString();
        }

        return (code, name, symbol);
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task<int> DeleteMissingUnitsAsync(HashSet<string> incomingExternalIds, Guid runId, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "UnitOfMeasure";
        const string linkEntityType = nameof(UnitOfMeasure);
        const string externalSystem = "Component2020";
        const string externalEntity = "Unit";

        var missingLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.EntityId, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (missingLinks.Count == 0)
        {
            return 0;
        }

        var externalIdByEntityId = new Dictionary<Guid, string>();
        foreach (var link in missingLinks)
        {
            if (!externalIdByEntityId.ContainsKey(link.EntityId))
            {
                externalIdByEntityId[link.EntityId] = link.ExternalId;
            }
        }

        var affectedEntityIds = missingLinks.Select(x => x.EntityId).Distinct().ToList();

        var allLinksForAffectedEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && affectedEntityIds.Contains(l.EntityId))
            .Select(l => new { l.Id, l.EntityId })
            .ToListAsync(cancellationToken);

        var missingLinkIds = missingLinks.Select(x => x.Id).ToHashSet();

        var entityIdsToDelete = allLinksForAffectedEntities
            .GroupBy(x => x.EntityId)
            .Where(g => g.All(x => missingLinkIds.Contains(x.Id)))
            .Select(g => g.Key)
            .ToHashSet();

        var referencedByItems = await _dbContext.Items
            .Where(i => affectedEntityIds.Contains(i.UnitOfMeasureId))
            .Select(i => i.UnitOfMeasureId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referencedByLines = await _dbContext.RequestLines
            .Where(l => l.UnitOfMeasureId != null && affectedEntityIds.Contains(l.UnitOfMeasureId.Value))
            .Select(l => l.UnitOfMeasureId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referenced = referencedByItems.Concat(referencedByLines).ToHashSet();

        if (referenced.Count > 0)
        {
            foreach (var unitId in referenced)
            {
                externalIdByEntityId.TryGetValue(unitId, out var externalId);
                errors.Add(new Component2020SyncError(
                    runId,
                    entityType,
                    null,
                    externalId ?? unitId.ToString(),
                    "Cannot delete unit because it is referenced by existing documents/items.",
                    null));
            }

            foreach (var unitId in referenced)
            {
                entityIdsToDelete.Remove(unitId);
            }
        }

        var linkIdsToDeleteOnly = missingLinks
            .Where(x => !entityIdsToDelete.Contains(x.EntityId))
            .Where(x => !referenced.Contains(x.EntityId))
            .Select(x => x.Id)
            .ToList();

        if (linkIdsToDeleteOnly.Count > 0)
        {
            var links = await _dbContext.ExternalEntityLinks
                .Where(l => linkIdsToDeleteOnly.Contains(l.Id))
                .ToListAsync(cancellationToken);

            _dbContext.ExternalEntityLinks.RemoveRange(links);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (entityIdsToDelete.Count == 0)
        {
            return 0;
        }

        var toDeleteIds = entityIdsToDelete.ToList();
        var linksToDeleteWithEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && toDeleteIds.Contains(l.EntityId))
            .ToListAsync(cancellationToken);

        var unitsToDelete = await _dbContext.UnitOfMeasures
            .Where(u => toDeleteIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(linksToDeleteWithEntities);
        _dbContext.UnitOfMeasures.RemoveRange(unitsToDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return unitsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for UnitOfMeasure overwrite cleanup, falling back to per-row deletes");
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var unitId in toDeleteIds)
            {
                try
                {
                    var current = await _dbContext.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    var currentLinks = await _dbContext.ExternalEntityLinks
                        .Where(l => l.EntityType == linkEntityType && l.EntityId == unitId)
                        .ToListAsync(cancellationToken);
                    _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                    _dbContext.UnitOfMeasures.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete UnitOfMeasure {UnitId} during overwrite cleanup", unitId);
                    externalIdByEntityId.TryGetValue(unitId, out var externalId);
                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        externalId ?? unitId.ToString(),
                        "Cannot delete unit during overwrite cleanup.",
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }
}

