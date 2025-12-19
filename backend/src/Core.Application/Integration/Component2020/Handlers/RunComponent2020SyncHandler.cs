using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;

namespace MyIS.Core.Application.Integration.Component2020.Handlers;

public class RunComponent2020SyncHandler
{
    private readonly IComponent2020SyncService _syncService;

    public RunComponent2020SyncHandler(IComponent2020SyncService syncService)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    public async Task<RunComponent2020SyncResponse> Handle(RunComponent2020SyncCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        return await _syncService.RunSyncAsync(command, cancellationToken);
    }
}