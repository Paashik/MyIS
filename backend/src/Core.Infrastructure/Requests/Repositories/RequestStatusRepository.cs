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

public sealed class RequestStatusRepository : IRequestStatusRepository
{
    private readonly AppDbContext _dbContext;

    public RequestStatusRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RequestStatus?> GetByIdAsync(RequestStatusId id, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestStatuses
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<RequestStatus?> GetByCodeAsync(RequestStatusCode code, CancellationToken cancellationToken)
    {
        var value = code.Value;
        return await _dbContext.RequestStatuses
            .FirstOrDefaultAsync(s => s.Code.Value == value, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestStatus>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.RequestStatuses
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return items;
    }
}