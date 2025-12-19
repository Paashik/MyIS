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

public class Component2020SyncRunRepository : IComponent2020SyncRunRepository
{
    private readonly AppDbContext _dbContext;

    public Component2020SyncRunRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Component2020SyncRunDto run, CancellationToken cancellationToken)
    {
        var entity = new Component2020SyncRun(run.Scope, run.Mode, run.StartedByUserId);
        entity.Complete(run.Status, run.ProcessedCount, run.ErrorCount, run.CountersJson, run.Summary);

        _dbContext.Component2020SyncRuns.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GetComponent2020SyncRunsResponse> GetRunsAsync(GetComponent2020SyncRunsQuery query, CancellationToken cancellationToken)
    {
        var queryable = _dbContext.Component2020SyncRuns.AsQueryable();

        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(r => r.StartedAt >= query.FromDate.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            queryable = queryable.Where(r => r.Status == query.Status);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var runs = await queryable
            .OrderByDescending(r => r.StartedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new Component2020SyncRunDto
            {
                Id = r.Id,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                StartedByUserId = r.StartedByUserId,
                Scope = r.Scope,
                Mode = r.Mode,
                Status = r.Status,
                ProcessedCount = r.ProcessedCount,
                ErrorCount = r.ErrorCount,
                CountersJson = r.CountersJson,
                Summary = r.Summary
            })
            .ToListAsync(cancellationToken);

        return new GetComponent2020SyncRunsResponse
        {
            Runs = runs.ToArray(),
            TotalCount = totalCount
        };
    }

    public async Task<Component2020SyncRunDto?> GetLastSuccessfulRunAsync(Component2020SyncScope scope, CancellationToken cancellationToken)
    {
        var run = await _dbContext.Component2020SyncRuns
            .Where(r => r.Status == "Success" && r.Scope == scope.ToString())
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (run == null) return null;

        return new Component2020SyncRunDto
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            StartedByUserId = run.StartedByUserId,
            Scope = run.Scope,
            Mode = run.Mode,
            Status = run.Status,
            ProcessedCount = run.ProcessedCount,
            ErrorCount = run.ErrorCount,
            CountersJson = run.CountersJson,
            Summary = run.Summary
        };
    }

    public async Task<GetComponent2020SyncRunErrorsResponse> GetRunErrorsAsync(GetComponent2020SyncRunErrorsQuery query, CancellationToken cancellationToken)
    {
        var errors = await _dbContext.Component2020SyncErrors
            .Where(e => e.SyncRunId == query.RunId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new Component2020SyncErrorDto
            {
                Id = e.Id,
                EntityType = e.EntityType,
                ExternalEntity = e.ExternalEntity,
                ExternalKey = e.ExternalKey,
                Message = e.Message,
                Details = e.Details,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetComponent2020SyncRunErrorsResponse
        {
            Errors = errors.ToArray()
        };
    }
}