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

public sealed class RequestTypeRepository : IRequestTypeRepository
{
    private readonly AppDbContext _dbContext;

    public RequestTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RequestType?> GetByIdAsync(RequestTypeId id, CancellationToken cancellationToken)
    {
        return await _dbContext.RequestTypes
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestType>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.RequestTypes
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return items;
    }
}