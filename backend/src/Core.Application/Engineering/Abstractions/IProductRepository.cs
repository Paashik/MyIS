using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Product?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        ProductType? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> AnyWithItemIdAsync(Guid itemId, CancellationToken cancellationToken);
}