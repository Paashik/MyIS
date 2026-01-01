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

public sealed class BomLineRepository : IBomLineRepository
{
    private readonly AppDbContext _dbContext;

    public BomLineRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<BomLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.BomLines
            .Include(bl => bl.BomVersion)
            .FirstOrDefaultAsync(bl => bl.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BomLine>> GetByBomVersionIdAsync(Guid bomVersionId, CancellationToken cancellationToken)
    {
        return await _dbContext.BomLines
            .Include(bl => bl.BomVersion)
            .Where(bl => bl.BomVersionId == bomVersionId)
            .OrderBy(bl => bl.PositionNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BomLine>> GetByParentItemIdAsync(Guid parentItemId, CancellationToken cancellationToken)
    {
        return await _dbContext.BomLines
            .Include(bl => bl.BomVersion)
            .Where(bl => bl.ParentItemId == parentItemId)
            .OrderBy(bl => bl.PositionNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BomLine>> GetByParentItemIdAsync(Guid bomVersionId, Guid parentItemId, bool onlyErrors, CancellationToken cancellationToken)
    {
        var query = _dbContext.BomLines
            .Include(bl => bl.BomVersion)
            .Where(bl => bl.BomVersionId == bomVersionId && bl.ParentItemId == parentItemId);

        if (onlyErrors)
        {
            query = query.Where(bl => bl.Status == LineStatus.Error || bl.Status == LineStatus.Warning);
        }

        return await query
            .OrderBy(bl => bl.PositionNo)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BomLine bomLine, CancellationToken cancellationToken)
    {
        if (bomLine is null) throw new ArgumentNullException(nameof(bomLine));

        await _dbContext.BomLines.AddAsync(bomLine, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BomLine bomLine, CancellationToken cancellationToken)
    {
        if (bomLine is null) throw new ArgumentNullException(nameof(bomLine));

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var bomLine = await _dbContext.BomLines.FindAsync(new object[] { id }, cancellationToken);
        if (bomLine != null)
        {
            _dbContext.BomLines.Remove(bomLine);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<BomLine> Items, int TotalCount)> SearchAsync(
        Guid? bomVersionId,
        Guid? parentItemId,
        Guid? itemId,
        BomRole? role,
        LineStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.BomLines
            .Include(bl => bl.BomVersion)
            .AsQueryable();

        if (bomVersionId.HasValue)
        {
            query = query.Where(bl => bl.BomVersionId == bomVersionId.Value);
        }

        if (parentItemId.HasValue)
        {
            query = query.Where(bl => bl.ParentItemId == parentItemId.Value);
        }

        if (itemId.HasValue)
        {
            query = query.Where(bl => bl.ItemId == itemId.Value);
        }

        if (role.HasValue)
        {
            query = query.Where(bl => bl.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(bl => bl.Status == status.Value);
        }

        query = query.OrderBy(bl => bl.PositionNo);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}