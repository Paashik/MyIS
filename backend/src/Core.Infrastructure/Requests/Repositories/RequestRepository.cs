using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
            .Include(r => r.Lines)
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

        // Only persist request comments here to avoid optimistic concurrency on request row.
        var newComments = request.Comments
            .Where(comment => _dbContext.Entry(comment).State == EntityState.Detached)
            .ToList();

        if (newComments.Count > 0)
        {
            await _dbContext.RequestComments.AddRangeAsync(newComments, cancellationToken);
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(RequestId id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.Requests.FindAsync(new object[] { id }, cancellationToken);
        if (request != null)
        {
            _dbContext.Requests.Remove(request);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(
        Guid? requestTypeId,
        Guid? requestStatusId,
        RequestDirection? direction,
        Guid? managerId,
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

        if (direction.HasValue)
        {
            query = query.Where(r => r.Type != null && r.Type.Direction == direction.Value);
        }

        if (requestStatusId.HasValue)
        {
            var statusId = new RequestStatusId(requestStatusId.Value);
            query = query.Where(r => r.RequestStatusId == statusId);
        }

        if (managerId.HasValue)
        {
            query = query.Where(r => r.ManagerId == managerId.Value);
        }

                // РџСЂРѕСЃС‚Р°СЏ СЃРѕСЂС‚РёСЂРѕРІРєР°: РїРѕ CreatedAt РїРѕ СѓР±С‹РІР°РЅРёСЋ, Р·Р°С‚РµРј РїРѕ РёРґРµРЅС‚РёС„РёРєР°С‚РѕСЂСѓ
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

    public async Task<bool> AnyWithTypeIdAsync(RequestTypeId requestTypeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Requests
            .AsNoTracking()
            .AnyAsync(r => r.RequestTypeId == requestTypeId, cancellationToken);
    }

    public async Task<bool> AnyWithStatusIdAsync(RequestStatusId requestStatusId, CancellationToken cancellationToken)
    {
        return await _dbContext.Requests
            .AsNoTracking()
            .AnyAsync(r => r.RequestStatusId == requestStatusId, cancellationToken);
    }

    public async Task<long> GetNextRequestNumberAsync(
        RequestDirection direction,
        int year,
        CancellationToken cancellationToken)
    {
        if (string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
        {
            var currentCount = await _dbContext.Requests
                .Include(r => r.Type)
                .LongCountAsync(
                    r => r.Type != null
                        && r.Type.Direction == direction
                        && r.CreatedAt.Year == year,
                    cancellationToken);
            return currentCount + 1;
        }

        if (year < 1) throw new ArgumentOutOfRangeException(nameof(year));

        var directionKey = direction == RequestDirection.Incoming ? "in" : "out";
        var sequenceName = $"requests.request_number_{directionKey}_{year}";

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START WITH 1 INCREMENT BY 1;";

        var connection = command.Connection ?? throw new InvalidOperationException("Database connection is not initialized.");
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);

        command.CommandText = $"SELECT nextval('{sequenceName}')";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
}




