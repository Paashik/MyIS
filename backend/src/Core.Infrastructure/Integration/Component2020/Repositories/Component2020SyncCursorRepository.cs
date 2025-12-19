using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Repositories;

public class Component2020SyncCursorRepository : IComponent2020SyncCursorRepository
{
    private readonly AppDbContext _dbContext;

    public Component2020SyncCursorRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<string?> GetLastProcessedKeyAsync(Guid connectionId, string sourceEntity, CancellationToken cancellationToken)
    {
        var cursor = await _dbContext.Component2020SyncCursors
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && c.SourceEntity == sourceEntity, cancellationToken);

        return cursor?.LastProcessedKey;
    }

    public async Task UpsertCursorAsync(Guid connectionId, string sourceEntity, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var cursor = await _dbContext.Component2020SyncCursors
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && c.SourceEntity == sourceEntity, cancellationToken);

        if (cursor == null)
        {
            cursor = new Component2020SyncCursor(connectionId, sourceEntity);
            _dbContext.Component2020SyncCursors.Add(cursor);
        }

        cursor.UpdateCursor(lastProcessedKey);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}