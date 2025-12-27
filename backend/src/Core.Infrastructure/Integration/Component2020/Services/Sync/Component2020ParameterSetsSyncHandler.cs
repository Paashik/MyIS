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

public sealed class Component2020ParameterSetsSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020ParameterSetsSyncHandler> _logger;

    public Component2020ParameterSetsSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020ParameterSetsSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.ParameterSets;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "ParameterSet";
        const string sourceEntity = "ParameterSets";
        const string externalSystem = "Component2020";
        const string externalEntity = "SPar";
        const string linkEntityType = nameof(ParameterSet);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var parameterSets = await _deltaReader.ReadParameterSetsDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, ParameterSet> existingById;

        if (!dryRun)
        {
            var externalIds = parameterSets.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.ParameterSets
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingById = new Dictionary<Guid, ParameterSet>();
        }

        foreach (var parameterSet in parameterSets)
        {
            try
            {
                var externalId = parameterSet.Id.ToString();
                incomingExternalIds?.Add(externalId);

                ParameterSet? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new ParameterSet(parameterSet.Name, parameterSet.Name, parameterSet.P0Id, parameterSet.P1Id, parameterSet.P2Id, parameterSet.P3Id, parameterSet.P4Id, parameterSet.P5Id);
                        _dbContext.ParameterSets.Add(created);
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.Update(parameterSet.Name, parameterSet.P0Id, parameterSet.P1Id, parameterSet.P2Id, parameterSet.P3Id, parameterSet.P4Id, parameterSet.P5Id, true);
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), parameterSet.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing parameter set {ParameterSetId}", parameterSet.Id);
                var error = new Component2020SyncError(runId, entityType, null, parameterSet.Id.ToString(), ex.Message, ex.StackTrace);
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
            var deleted = await _externalLinkHelper.DeleteMissingByExternalLinkAsync(
                _dbContext.ParameterSets,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ParameterSetDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }
}

