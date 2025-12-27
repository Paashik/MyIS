using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020ItemGroupsSyncHandler : Component2020ItemSyncHandlerBase, IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<Component2020ItemGroupsSyncHandler> _logger;

    public Component2020ItemGroupsSyncHandler(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020ItemGroupsSyncHandler> logger)
        : base(dbContext, snapshotReader, externalLinkHelper, logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.ItemGroups;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "ItemGroup";
        const string externalSystem = "Component2020";
        const string externalEntity = "Groups";
        const string linkEntityType = nameof(ItemGroup);

        _logger.LogInformation("Starting ItemGroups sync (connectionId={ConnectionId}, dryRun={DryRun}, syncMode={SyncMode})",
            connectionId, dryRun, syncMode);

        var groups = (await SnapshotReader.ReadItemGroupsAsync(cancellationToken, connectionId)).ToList();
        _logger.LogInformation("Read {Count} groups from Component2020 for sync", groups.Count);

        var processed = groups.Count;

        var incomingExternalIds = syncMode == Component2020SyncMode.Overwrite
            ? new HashSet<string>(groups.Select(g => g.Id.ToString()), StringComparer.Ordinal)
            : null;

        await using var transaction = !dryRun ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        try
        {
            _logger.LogInformation("Calling EnsureItemGroupsAsync to process groups");
            await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
            _logger.LogInformation("EnsureItemGroupsAsync completed successfully");

            if (!dryRun && syncMode == Component2020SyncMode.Overwrite && incomingExternalIds != null)
            {
                _logger.LogInformation("Performing overwrite cleanup for missing groups");
                var deleted = await ExternalLinkHelper.DeleteMissingByExternalLinkAsync(
                    _dbContext.ItemGroups,
                    linkEntityType,
                    externalSystem,
                    externalEntity,
                    incomingExternalIds,
                    runId,
                    entityType,
                    errors,
                    cancellationToken);

                _logger.LogInformation("Overwrite cleanup completed: deleted {Count} groups", deleted);
                counters[$"{entityType}Deleted"] = deleted;
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Transaction committed successfully");
            }

            counters[entityType] = processed;
            _logger.LogInformation("ItemGroups sync completed: processed={Processed}, errors={Errors}", processed, errors.Count);
            return (processed, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ItemGroups sync");
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogInformation("Transaction rolled back due to error");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
            }
            throw;
        }
    }
}

