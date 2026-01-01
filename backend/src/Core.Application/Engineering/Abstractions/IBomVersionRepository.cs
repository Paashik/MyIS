using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Abstractions;

public interface IBomVersionRepository
{
    Task<BomVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BomVersion?> GetByProductIdAndVersionCodeAsync(Guid productId, string versionCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<BomVersion>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task AddAsync(BomVersion bomVersion, CancellationToken cancellationToken);

    Task UpdateAsync(BomVersion bomVersion, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<BomVersion> Items, int TotalCount)> SearchAsync(
        Guid? productId,
        BomStatus? status,
        BomSource? source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}