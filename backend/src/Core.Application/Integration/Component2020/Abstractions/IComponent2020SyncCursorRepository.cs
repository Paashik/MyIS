using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Integration.Component2020.Abstractions;

public interface IComponent2020SyncCursorRepository
{
    Task<string?> GetLastProcessedKeyAsync(Guid connectionId, string sourceEntity, CancellationToken cancellationToken);
    Task UpsertCursorAsync(Guid connectionId, string sourceEntity, string? lastProcessedKey, CancellationToken cancellationToken);
}