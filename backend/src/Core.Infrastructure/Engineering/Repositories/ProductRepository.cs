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

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(p => p.BomVersions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(p => p.BomVersions)
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<Product?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(p => p.BomVersions)
            .FirstOrDefaultAsync(p => p.ItemId == itemId, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        if (product is null) throw new ArgumentNullException(nameof(product));

        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        if (product is null) throw new ArgumentNullException(nameof(product));

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product != null)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        ProductType? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.BomVersions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Code.Contains(searchTerm) || p.Name.Contains(searchTerm));
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        query = query.OrderBy(p => p.Code);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> AnyWithItemIdAsync(Guid itemId, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.ItemId == itemId, cancellationToken);
    }
}