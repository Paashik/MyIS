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

public sealed class BomVersionRepository : IBomVersionRepository
{
    private readonly AppDbContext _dbContext;

    public BomVersionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<BomVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.BomVersions
            .Include(bv => bv.Product)
            .Include(bv => bv.Lines)
            .Include(bv => bv.Operations)
            .FirstOrDefaultAsync(bv => bv.Id == id, cancellationToken);
    }

    public async Task<BomVersion?> GetByProductIdAndVersionCodeAsync(Guid productId, string versionCode, CancellationToken cancellationToken)
    {
        return await _dbContext.BomVersions
            .Include(bv => bv.Product)
            .Include(bv => bv.Lines)
            .Include(bv => bv.Operations)
            .FirstOrDefaultAsync(bv => bv.ProductId == productId && bv.VersionCode == versionCode, cancellationToken);
    }

    public async Task<IReadOnlyList<BomVersion>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await _dbContext.BomVersions
            .Include(bv => bv.Product)
            .Where(bv => bv.ProductId == productId)
            .OrderBy(bv => bv.VersionCode)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BomVersion bomVersion, CancellationToken cancellationToken)
    {
        if (bomVersion is null) throw new ArgumentNullException(nameof(bomVersion));

        await _dbContext.BomVersions.AddAsync(bomVersion, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BomVersion bomVersion, CancellationToken cancellationToken)
    {
        if (bomVersion is null) throw new ArgumentNullException(nameof(bomVersion));

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var bomVersion = await _dbContext.BomVersions.FindAsync(new object[] { id }, cancellationToken);
        if (bomVersion != null)
        {
            _dbContext.BomVersions.Remove(bomVersion);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<BomVersion> Items, int TotalCount)> SearchAsync(
        Guid? productId,
        BomStatus? status,
        BomSource? source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.BomVersions
            .Include(bv => bv.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(bv => bv.ProductId == productId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(bv => bv.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(bv => bv.Source == source.Value);
        }

        query = query.OrderByDescending(bv => bv.VersionCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}