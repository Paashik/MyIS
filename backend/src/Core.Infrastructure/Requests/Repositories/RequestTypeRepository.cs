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

public sealed class RequestTypeRepository : IRequestTypeRepository
{
    private readonly AppDbContext _dbContext;

    public RequestTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RequestType?> GetByIdAsync(RequestTypeId id, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<RequestType?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.RequestTypes
            .FirstOrDefaultAsync(t => t.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestType>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _dbContext.RequestTypes.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        var items = await query
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.RequestTypes
            .AnyAsync(t => t.Code == normalized, cancellationToken);
    }

    public async Task AddAsync(RequestType type, CancellationToken cancellationToken)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        await _dbContext.RequestTypes.AddAsync(type, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RequestType type, CancellationToken cancellationToken)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));

        _dbContext.RequestTypes.Update(type);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
