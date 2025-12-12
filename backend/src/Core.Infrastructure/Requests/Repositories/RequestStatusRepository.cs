using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Requests.Repositories;

public sealed class RequestStatusRepository : IRequestStatusRepository
{
    private readonly AppDbContext _dbContext;

    public RequestStatusRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RequestStatus?> GetByIdAsync(RequestStatusId id, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestStatuses
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<RequestStatus?> GetByCodeAsync(RequestStatusCode code, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestStatuses
            // IMPORTANT: compare by VO itself so EF Core can apply the configured ValueConverter.
            // Accessing s.Code.Value is not translatable and causes runtime errors.
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestStatus>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _dbContext.RequestStatuses.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var items = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<bool> ExistsByCodeAsync(RequestStatusCode code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code.Value))
        {
            return false;
        }

        return await _dbContext.RequestStatuses
            .AnyAsync(s => s.Code == code, cancellationToken);
    }

    public async Task AddAsync(RequestStatus status, CancellationToken cancellationToken)
    {
        if (status is null) throw new ArgumentNullException(nameof(status));

        await _dbContext.RequestStatuses.AddAsync(status, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RequestStatus status, CancellationToken cancellationToken)
    {
        if (status is null) throw new ArgumentNullException(nameof(status));

        _dbContext.RequestStatuses.Update(status);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
