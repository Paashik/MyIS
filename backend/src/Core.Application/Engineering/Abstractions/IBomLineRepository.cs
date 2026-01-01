using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Abstractions;

public interface IBomLineRepository
{
    Task<BomLine?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BomLine>> GetByBomVersionIdAsync(Guid bomVersionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BomLine>> GetByParentItemIdAsync(Guid parentItemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BomLine>> GetByParentItemIdAsync(Guid bomVersionId, Guid parentItemId, bool onlyErrors, CancellationToken cancellationToken);

    Task AddAsync(BomLine bomLine, CancellationToken cancellationToken);

    Task UpdateAsync(BomLine bomLine, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<BomLine> Items, int TotalCount)> SearchAsync(
        Guid? bomVersionId,
        Guid? parentItemId,
        Guid? itemId,
        BomRole? role,
        LineStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}