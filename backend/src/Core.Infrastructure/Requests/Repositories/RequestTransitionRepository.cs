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

public sealed class RequestTransitionRepository : IRequestTransitionRepository
{
    private readonly AppDbContext _dbContext;

    public RequestTransitionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<RequestTransition>> GetByTypeAndFromStatusAsync(
        RequestTypeId requestTypeId,
        RequestStatusCode fromStatusCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RequestTransitions
            .AsNoTracking()
            .Where(t => t.RequestTypeId == requestTypeId)
            .Where(t => t.FromStatusCode == fromStatusCode)
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.ActionCode)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<RequestTransition?> FindByTypeFromStatusAndActionAsync(
        RequestTypeId requestTypeId,
        RequestStatusCode fromStatusCode,
        string actionCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actionCode))
        {
            throw new ArgumentException("ActionCode is required.", nameof(actionCode));
        }

        var normalized = actionCode.Trim();

        return await _dbContext.RequestTransitions
            .AsNoTracking()
            .Where(t => t.RequestTypeId == requestTypeId)
            .Where(t => t.FromStatusCode == fromStatusCode)
            .Where(t => t.IsEnabled)
            .FirstOrDefaultAsync(t => t.ActionCode == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestTransition>> GetAllByTypeAsync(
        RequestTypeId requestTypeId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.RequestTransitions
            .AsNoTracking()
            .Where(t => t.RequestTypeId == requestTypeId)
            .AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(t => t.IsEnabled);
        }

        return await query
            // IMPORTANT: avoid ordering by ValueObject.Value to keep the query translatable.
            .OrderBy(t => t.ActionCode)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequestTransition>> GetAllAsync(
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.RequestTransitions
            .AsNoTracking()
            .AsQueryable();

        if (!includeDisabled)
        {
            query = query.Where(t => t.IsEnabled);
        }

        return await query
            // IMPORTANT: avoid ordering by ValueObject.Value to keep the query translatable.
            .OrderBy(t => t.ActionCode)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForTypeAsync(
        RequestTypeId requestTypeId,
        IReadOnlyList<RequestTransition> newTransitions,
        CancellationToken cancellationToken)
    {
        if (requestTypeId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestTypeId cannot be empty.", nameof(requestTypeId));
        }

        if (newTransitions is null)
        {
            throw new ArgumentNullException(nameof(newTransitions));
        }

        // IMPORTANT: track existing transitions to update/remove.
        var existing = await _dbContext.RequestTransitions
            .Where(t => t.RequestTypeId == requestTypeId)
            .ToListAsync(cancellationToken);

        static string BuildKey(RequestStatusCode fromStatusCode, string actionCode)
            => $"{fromStatusCode.Value}::{actionCode.Trim()}";

        var existingByKey = existing.ToDictionary(
            t => BuildKey(t.FromStatusCode, t.ActionCode),
            StringComparer.OrdinalIgnoreCase);

        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in newTransitions)
        {
            if (t.RequestTypeId != requestTypeId)
            {
                throw new InvalidOperationException("All transitions must have the same RequestTypeId as the target type.");
            }

            var key = BuildKey(t.FromStatusCode, t.ActionCode);
            if (!incomingKeys.Add(key))
            {
                throw new InvalidOperationException($"Duplicate transition key '{key}'.");
            }

            if (existingByKey.TryGetValue(key, out var current))
            {
                current.ChangeToStatus(t.ToStatusCode);
                current.ChangeRequiredPermission(t.RequiredPermission);
                if (t.IsEnabled) current.Enable(); else current.Disable();
            }
            else
            {
                await _dbContext.RequestTransitions.AddAsync(t, cancellationToken);
            }
        }

        foreach (var t in existing)
        {
            var key = BuildKey(t.FromStatusCode, t.ActionCode);
            if (!incomingKeys.Contains(key))
            {
                _dbContext.RequestTransitions.Remove(t);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AnyUsesStatusCodeAsync(RequestStatusCode statusCode, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestTransitions
            .AsNoTracking()
            .AnyAsync(
                t => t.FromStatusCode == statusCode || t.ToStatusCode == statusCode,
                cancellationToken);
    }
}

