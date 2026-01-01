using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Abstractions;

public interface IBomOperationRepository
{
    Task<BomOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BomOperation>> GetByBomVersionIdAsync(Guid bomVersionId, CancellationToken cancellationToken);

    Task AddAsync(BomOperation bomOperation, CancellationToken cancellationToken);

    Task UpdateAsync(BomOperation bomOperation, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<BomOperation> Items, int TotalCount)> SearchAsync(
        Guid? bomVersionId,
        string? areaName,
        OperationStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}