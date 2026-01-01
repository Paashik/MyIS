using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Common;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public class Component2020SyncService : IComponent2020SyncService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<Component2020SyncService> _logger;
    private readonly IReadOnlyDictionary<Component2020SyncScope, IComponent2020SyncHandler> _handlersByScope;

    public Component2020SyncService(
        AppDbContext dbContext,
        ILogger<Component2020SyncService> logger,
        IEnumerable<IComponent2020SyncHandler> handlers)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlersByScope = handlers?.ToDictionary(h => h.Scope)
            ?? throw new ArgumentNullException(nameof(handlers));
    }

    public async Task<RunComponent2020SyncResponse> RunSyncAsync(RunComponent2020SyncCommand command, CancellationToken cancellationToken)
    {
        var runMode = $"{(command.DryRun ? "DryRun" : "Commit")}:{command.SyncMode}";
        var run = new Component2020SyncRun(command.Scope.ToString(), runMode, command.StartedByUserId);

        _dbContext.Component2020SyncRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var counters = new Dictionary<string, int>();
            var errors = new List<Component2020SyncError>();

            if (command.Scope == Component2020SyncScope.Units || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Units, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Counterparties || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Counterparties, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.ItemGroups || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.ItemGroups, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Products || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Products, command, run.Id, counters, errors, cancellationToken);
            }
            
            if (command.Scope == Component2020SyncScope.Items || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Items, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Bom || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Bom, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Manufacturers || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Manufacturers, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.BodyTypes || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.BodyTypes, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Currencies || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Currencies, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.TechnicalParameters || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.TechnicalParameters, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.ParameterSets || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.ParameterSets, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Symbols || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Symbols, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Employees || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Employees, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Users || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Users, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.CustomerOrders || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.CustomerOrders, command, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Statuses || command.Scope == Component2020SyncScope.All)
            {
                await RunScopeAsync(Component2020SyncScope.Statuses, command, run.Id, counters, errors, cancellationToken);
            }

            var totalProcessed = counters.Values.Sum();
            var totalErrors = errors.Count;

            string status = totalErrors == 0 ? "Success" : (totalProcessed > 0 ? "Partial" : "Failed");

            run.Complete(status, totalProcessed, totalErrors, System.Text.Json.JsonSerializer.Serialize(counters), null);

            if (errors.Count > 0)
            {
                _dbContext.Component2020SyncErrors.AddRange(errors);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RunComponent2020SyncResponse
            {
                RunId = run.Id,
                Status = status,
                ProcessedCount = totalProcessed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Component2020 sync");

            run.Complete("Failed", 0, 1, null, ex.Message);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RunComponent2020SyncResponse
            {
                RunId = run.Id,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task RunScopeAsync(
        Component2020SyncScope scope,
        RunComponent2020SyncCommand command,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        if (!_handlersByScope.TryGetValue(scope, out var handler))
        {
            throw new InvalidOperationException($"Missing Component2020 sync handler for {scope}.");
        }

        await handler.SyncAsync(command.ConnectionId, command.DryRun, command.SyncMode, runId, counters, errors, cancellationToken);
    }
}
