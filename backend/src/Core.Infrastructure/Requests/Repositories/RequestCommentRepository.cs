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

public sealed class RequestCommentRepository : IRequestCommentRepository
{
    private readonly AppDbContext _dbContext;

    public RequestCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<RequestComment>> GetByRequestIdAsync(
        RequestId requestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RequestComments
            .Where(c => c.RequestId == requestId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RequestComment comment, CancellationToken cancellationToken)
    {
        if (comment is null) throw new ArgumentNullException(nameof(comment));

        await _dbContext.RequestComments.AddAsync(comment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}