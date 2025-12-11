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

public sealed class RequestAttachmentRepository : IRequestAttachmentRepository
{
    private readonly AppDbContext _dbContext;

    public RequestAttachmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<RequestAttachment>> GetByRequestIdAsync(
        RequestId requestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RequestAttachments
            .Where(a => a.RequestId == requestId)
            .OrderBy(a => a.UploadedAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RequestAttachment attachment, CancellationToken cancellationToken)
    {
        if (attachment is null) throw new ArgumentNullException(nameof(attachment));

        await _dbContext.RequestAttachments.AddAsync(attachment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}