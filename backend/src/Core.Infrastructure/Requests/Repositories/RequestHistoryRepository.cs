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

public sealed class RequestHistoryRepository : IRequestHistoryRepository
{
    private readonly AppDbContext _dbContext;

    public RequestHistoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<RequestHistory>> GetByRequestIdAsync(
        RequestId requestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RequestHistory
            .Where(h => h.RequestId == requestId)
            .OrderBy(h => h.Timestamp)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);
    }
}
