using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Domain.Engineering.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Engineering.Repositories;

public sealed class BomOperationRepository : IBomOperationRepository
{
    private readonly AppDbContext _dbContext;

    public BomOperationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<BomOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.BomOperations
            .Include(bo => bo.BomVersion)
            .FirstOrDefaultAsync(bo => bo.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BomOperation>> GetByBomVersionIdAsync(Guid bomVersionId, CancellationToken cancellationToken)
    {
        return await _dbContext.BomOperations
            .Include(bo => bo.BomVersion)
            .Where(bo => bo.BomVersionId == bomVersionId)
            .OrderBy(bo => bo.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BomOperation bomOperation, CancellationToken cancellationToken)
    {
        if (bomOperation is null) throw new ArgumentNullException(nameof(bomOperation));

        await _dbContext.BomOperations.AddAsync(bomOperation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BomOperation bomOperation, CancellationToken cancellationToken)
    {
        if (bomOperation is null) throw new ArgumentNullException(nameof(bomOperation));

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var bomOperation = await _dbContext.BomOperations.FindAsync(new object[] { id }, cancellationToken);
        if (bomOperation != null)
        {
            _dbContext.BomOperations.Remove(bomOperation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<BomOperation> Items, int TotalCount)> SearchAsync(
        Guid? bomVersionId,
        string? areaName,
        OperationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.BomOperations
            .Include(bo => bo.BomVersion)
            .AsQueryable();

        if (bomVersionId.HasValue)
        {
            query = query.Where(bo => bo.BomVersionId == bomVersionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(areaName))
        {
            query = query.Where(bo => bo.AreaName == areaName);
        }

        if (status.HasValue)
        {
            query = query.Where(bo => bo.Status == status.Value);
        }

        query = query.OrderBy(bo => bo.Code);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}