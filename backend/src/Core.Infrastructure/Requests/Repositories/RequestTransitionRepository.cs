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
            .FirstOrDefaultAsync(t => t.ActionCode == normalized, cancellationToken);
    }
}

