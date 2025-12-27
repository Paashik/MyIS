using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Domain.Common;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020ExternalLinkHelper
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<Component2020ExternalLinkHelper> _logger;

    public Component2020ExternalLinkHelper(AppDbContext dbContext, ILogger<Component2020ExternalLinkHelper> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnsureExternalEntityLink(
        Dictionary<string, ExternalEntityLink> existingLinksByExternalId,
        string entityType,
        Guid entityId,
        string externalSystem,
        string externalEntity,
        string externalId,
        int? sourceType,
        DateTimeOffset now)
    {
        if (existingLinksByExternalId.TryGetValue(externalId, out var existing))
        {
            if (!string.Equals(existing.EntityType, entityType, StringComparison.Ordinal) || existing.EntityId != entityId)
            {
                throw new InvalidOperationException(
                    $"External link {externalSystem}:{externalEntity}:{externalId} is already linked to another {existing.EntityType}:{existing.EntityId}.");
            }

            existing.Touch(now, sourceType);
            return;
        }

        var link = new ExternalEntityLink(entityType, entityId, externalSystem, externalEntity, externalId, sourceType, now);
        _dbContext.ExternalEntityLinks.Add(link);
        existingLinksByExternalId[externalId] = link;
    }

    public async Task<int> DeleteMissingByExternalLinkAsync<TEntity>(
        DbSet<TEntity> set,
        string linkEntityType,
        string externalSystem,
        string externalEntity,
        HashSet<string> incomingExternalIds,
        Guid runId,
        string entityType,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
        where TEntity : class
    {
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
        var missingLinkIds = missingLinks.Select(x => x.Id).ToHashSet();

        var allLinksForAffectedEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && affectedEntityIds.Contains(l.EntityId))
            .Select(l => new { l.Id, l.EntityId })
            .ToListAsync(cancellationToken);

        var entityIdsToDeleteByLinks = allLinksForAffectedEntities
            .GroupBy(x => x.EntityId)
            .Where(g => g.All(x => missingLinkIds.Contains(x.Id)))
            .Select(g => g.Key)
            .ToHashSet();

        var entityIdsToDelete = entityIdsToDeleteByLinks;

        var linkIdsToDeleteOnly = missingLinks
            .Where(x => !entityIdsToDelete.Contains(x.EntityId))
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

        var ids = entityIdsToDelete.ToList();
        var linksToDeleteWithEntity = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && ids.Contains(l.EntityId))
            .ToListAsync(cancellationToken);
        var entitiesToDelete = await set
            .Where(e => ids.Contains(EF.Property<Guid>(e, "Id")))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(linksToDeleteWithEntity);
        set.RemoveRange(entitiesToDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entitiesToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for {EntityType} overwrite cleanup, falling back to per-row deletes", entityType);
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var entityId in ids)
            {
                try
                {
                    var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, cancellationToken);

                    var currentLinks = await _dbContext.ExternalEntityLinks
                        .Where(l => l.EntityType == linkEntityType && l.EntityId == entityId)
                        .ToListAsync(cancellationToken);

                    if (current == null)
                    {
                        // Orphan links only
                        _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                    set.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete {EntityType} {EntityId} during overwrite cleanup", entityType, entityId);

                    var message = "Cannot delete record during overwrite cleanup.";

                    try
                    {
                        var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, cancellationToken);
                        if (current != null)
                        {
                            if (current is IDeactivatable deactivatable)
                            {
                                deactivatable.Deactivate();
                                set.Update(current);
                                await _dbContext.SaveChangesAsync(cancellationToken);
                                message = "Record is referenced; deactivated instead of deleting during overwrite cleanup.";
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    externalIdByEntityId.TryGetValue(entityId, out var externalId);
                    externalId ??= entityId.ToString();

                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        externalId,
                        message,
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }
}
