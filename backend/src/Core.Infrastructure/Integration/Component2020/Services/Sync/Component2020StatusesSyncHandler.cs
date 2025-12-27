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
using MyIS.Core.Domain.Statuses.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020StatusesSyncHandler : IComponent2020SyncHandler
{
    private sealed record StatusGroupDefinition(int Kind, string Name, int SortOrder);

    private static readonly IReadOnlyDictionary<int, StatusGroupDefinition> StatusGroupDefinitionsByKind =
        new Dictionary<int, StatusGroupDefinition>
        {
            [0] = new(0, "Статусы компонентов", 0),
            [1] = new(1, "Статусы заказов поставщикам", 1),
            [2] = new(2, "Статусы заказов клиентов", 2),
            [3] = new(3, "Типы заказов клиентов", 3)
        };

    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020StatusesSyncHandler> _logger;

    public Component2020StatusesSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020StatusesSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Statuses;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "Status";
        const string sourceEntity = "Status";
        const string externalSystem = "Component2020";
        const string externalEntity = "Status";
        const string statusCodeExternalEntity = "StatusCode";
        const string linkEntityType = nameof(Status);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var statuses = (await _deltaReader.ReadStatusesDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var lastIdValue) ? lastIdValue : 0;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<string, ExternalEntityLink> existingCodeLinksByExternalId;
        Dictionary<Guid, Status> existingStatusesById;
        var statusGroupsByKind = new Dictionary<int, Status>();
        var statusGroupLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);

        if (!dryRun && statuses.Count > 0)
        {
            var externalIds = statuses.Select(s => s.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var statusCodeTokens = statuses
                .Where(s => s.Code.HasValue)
                .Select(s => s.Code!.Value.ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (statusCodeTokens.Count > 0)
            {
                var existingCodeLinks = await _dbContext.ExternalEntityLinks
                    .Where(l =>
                        l.EntityType == linkEntityType
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == statusCodeExternalEntity
                        && statusCodeTokens.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken);

                existingCodeLinksByExternalId = existingCodeLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);
            }
            else
            {
                existingCodeLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            }

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Statuses
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingStatusesById = existingEntities.ToDictionary(x => x.Id);

            var requiredKinds = statuses
                .Where(s => s.Kind.HasValue)
                .Select(s => s.Kind!.Value)
                .Distinct()
                .ToList();

            if (requiredKinds.Count > 0)
            {
                var requiredKindTokens = requiredKinds
                    .Select(k => k.ToString(CultureInfo.InvariantCulture))
                    .ToList();

                var existingGroupLinks = await _dbContext.ExternalEntityLinks
                    .Where(l =>
                        l.EntityType == linkEntityType
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == "StatusKind"
                        && requiredKindTokens.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken);

                statusGroupLinksByExternalId = existingGroupLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

                var groupIds = existingGroupLinks
                    .Select(x => x.EntityId)
                    .Distinct()
                    .ToList();

                if (groupIds.Count > 0)
                {
                    var existingGroups = await _dbContext.Statuses
                        .Where(x => groupIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var group in existingGroups)
                    {
                        var link = existingGroupLinks.FirstOrDefault(x => x.EntityId == group.Id);
                        if (link == null)
                        {
                            continue;
                        }

                        if (int.TryParse(link.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var kind))
                        {
                            statusGroupsByKind[kind] = group;
                        }
                    }
                }

                foreach (var definition in StatusGroupDefinitionsByKind.Values)
                {
                    if (!requiredKinds.Contains(definition.Kind))
                    {
                        continue;
                    }

                    if (!statusGroupsByKind.TryGetValue(definition.Kind, out var group))
                    {
                        group = new Status(null, definition.Name, description: null, color: null, flags: null, sortOrder: definition.SortOrder);
                        _dbContext.Statuses.Add(group);
                        statusGroupsByKind[definition.Kind] = group;
                    }
                    else
                    {
                        group.UpdateFromExternal(definition.Name, group.Description, group.Color, group.Flags, definition.SortOrder, true);
                    }

                    var now = DateTimeOffset.UtcNow;
                    _externalLinkHelper.EnsureExternalEntityLink(
                        statusGroupLinksByExternalId,
                        linkEntityType,
                        group.Id,
                        externalSystem,
                        "StatusKind",
                        definition.Kind.ToString(CultureInfo.InvariantCulture),
                        null,
                        now);
                }
            }
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingCodeLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingStatusesById = new Dictionary<Guid, Status>();
        }

        foreach (var status in statuses)
        {
            try
            {
                var externalId = status.Id.ToString();
                incomingExternalIds?.Add(externalId);

                if (!TryResolveStatusGroupDefinition(status.Kind, out var definition))
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Unknown status kind '{status.Kind}'.", null));
                    continue;
                }

                var resolvedDefinition = definition!;
                var name = string.IsNullOrWhiteSpace(status.Name) ? $"Status {status.Id}" : status.Name.Trim();

                Status? existing = null;
                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingStatusesById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        if (!statusGroupsByKind.TryGetValue(resolvedDefinition.Kind, out var group))
                        {
                            errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Missing status group for kind '{resolvedDefinition.Kind}'.", null));
                            continue;
                        }

                        var created = new Status(group.Id, name, description: null, status.Color, status.Flags, status.SortOrder);
                        _dbContext.Statuses.Add(created);
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                        if (status.Code.HasValue)
                        {
                            var codeExternalId = status.Code.Value.ToString(CultureInfo.InvariantCulture);
                            _externalLinkHelper.EnsureExternalEntityLink(existingCodeLinksByExternalId, linkEntityType, created.Id, externalSystem, statusCodeExternalEntity, codeExternalId, null, DateTimeOffset.UtcNow);
                        }
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        if (!statusGroupsByKind.TryGetValue(resolvedDefinition.Kind, out var group))
                        {
                            errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Missing status group for kind '{resolvedDefinition.Kind}'.", null));
                            continue;
                        }

                        existing.ChangeGroup(group.Id);
                        existing.UpdateFromExternal(name, existing.Description, status.Color, status.Flags, status.SortOrder, true);
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                        if (status.Code.HasValue)
                        {
                            var codeExternalId = status.Code.Value.ToString(CultureInfo.InvariantCulture);
                            _externalLinkHelper.EnsureExternalEntityLink(existingCodeLinksByExternalId, linkEntityType, existing.Id, externalSystem, statusCodeExternalEntity, codeExternalId, null, DateTimeOffset.UtcNow);
                        }
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, status.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing status {StatusId}", status.Id);
                var error = new Component2020SyncError(runId, entityType, null, status.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastId.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await _externalLinkHelper.DeleteMissingByExternalLinkAsync(
                _dbContext.Statuses,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["StatusDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private static bool TryResolveStatusGroupDefinition(int? kind, out StatusGroupDefinition? definition)
    {
        if (kind.HasValue && StatusGroupDefinitionsByKind.TryGetValue(kind.Value, out definition))
        {
            return true;
        }

        definition = null;
        return false;
    }
}

