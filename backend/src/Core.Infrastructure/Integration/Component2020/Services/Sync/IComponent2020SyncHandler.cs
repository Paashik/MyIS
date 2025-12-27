using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public interface IComponent2020SyncHandler
{
    Component2020SyncScope Scope { get; }

    Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken);
}

