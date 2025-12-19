using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Handlers;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Integration.Component2020.BackgroundServices;

public class Component2020SchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Component2020SchedulerHostedService> _logger;

    public Component2020SchedulerHostedService(
        IServiceProvider serviceProvider,
        ILogger<Component2020SchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Component2020 Scheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRunScheduledSyncsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Component2020 scheduler");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Check every 5 minutes
        }

        _logger.LogInformation("Component2020 Scheduler stopped");
    }

    private async Task CheckAndRunScheduledSyncsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var activeSchedules = await dbContext.Component2020SyncSchedules
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var schedule in activeSchedules)
        {
            // Check if next run is due and try to acquire lock
            if (!schedule.NextRunAt.HasValue || DateTimeOffset.UtcNow < schedule.NextRunAt.Value)
            {
                continue;
            }

            // Try to acquire lock by updating the schedule
            var rowsAffected = await dbContext.Component2020SyncSchedules
                .Where(s => s.Id == schedule.Id && s.NextRunAt == schedule.NextRunAt)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastRunAt, DateTimeOffset.UtcNow), cancellationToken);

            if (rowsAffected == 0)
            {
                // Another instance already picked it up
                continue;
            }

            try
            {
                var handler = scope.ServiceProvider.GetRequiredService<RunComponent2020SyncHandler>();
                var command = new RunComponent2020SyncCommand
                {
                    ConnectionId = Guid.NewGuid(), // TODO: get from connection provider or config
                    Scope = Enum.Parse<Component2020SyncScope>(schedule.Scope),
                    DryRun = false, // Default to commit mode
                    StartedByUserId = null // System user
                };

                var response = await handler.Handle(command, cancellationToken);

                // Update schedule with new next run
                schedule.MarkRun(DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Log error, but don't reset lock to avoid infinite retries
                throw;
            }
        }
    }
}