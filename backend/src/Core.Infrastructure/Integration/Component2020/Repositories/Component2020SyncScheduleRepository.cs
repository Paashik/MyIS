using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Queries;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Repositories;

public class Component2020SyncScheduleRepository : IComponent2020SyncScheduleRepository
{
    private readonly AppDbContext _dbContext;

    public Component2020SyncScheduleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Component2020SyncScheduleDto schedule, CancellationToken cancellationToken)
    {
        var entity = new Component2020SyncSchedule(schedule.Name, schedule.CronExpression, schedule.Scope.ToString());
        _dbContext.Component2020SyncSchedules.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<Component2020SyncScheduleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Component2020SyncSchedules.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;

        return new Component2020SyncScheduleDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CronExpression = entity.CronExpression,
            Scope = Enum.Parse<Component2020SyncScope>(entity.Scope),
            IsActive = entity.IsActive,
            LastRunAt = entity.LastRunAt,
            NextRunAt = entity.NextRunAt
        };
    }

    public async Task UpdateAsync(Component2020SyncScheduleDto schedule, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Component2020SyncSchedules.FindAsync(new object[] { schedule.Id }, cancellationToken);
        if (entity == null) throw new InvalidOperationException("Schedule not found");

        entity.Update(schedule.Name, schedule.CronExpression, schedule.Scope.ToString(), schedule.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSchedulesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Component2020SyncSchedules.AnyAsync(s => s.IsActive, cancellationToken);
    }
}