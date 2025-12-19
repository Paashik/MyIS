using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Commands;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020SyncService
{
    Task<RunComponent2020SyncResponse> RunSyncAsync(RunComponent2020SyncCommand command, CancellationToken cancellationToken);
}