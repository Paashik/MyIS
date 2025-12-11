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

public sealed class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _dbContext;

    public RequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken)
    {
        return await _dbContext.Requests
            .Include(r => r.Type)
            .Include(r => r.Status)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Request request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        await _dbContext.Requests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Request request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        _dbContext.Requests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(
        Guid? requestTypeId,
        Guid? requestStatusId,
        Guid? initiatorId,
        bool onlyMine,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbContext.Requests
            .Include(r => r.Type)
            .Include(r => r.Status)
            .AsQueryable();

        if (requestTypeId.HasValue)
        {
            var typeId = new RequestTypeId(requestTypeId.Value);
            query = query.Where(r => r.RequestTypeId == typeId);
        }

        if (requestStatusId.HasValue)
        {
            var statusId = new RequestStatusId(requestStatusId.Value);
            query = query.Where(r => r.RequestStatusId == statusId);
        }

        if (initiatorId.HasValue)
        {
            query = query.Where(r => r.InitiatorId == initiatorId.Value);
        }

                // Простая сортировка: по CreatedAt по убыванию, затем по идентификатору
                query = query
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenBy(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}