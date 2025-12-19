using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Queries;
using MyIS.Core.Application.Integration.Component2020.Services;

namespace MyIS.Core.Application.Integration.Component2020.Handlers;

public class GetComponent2020SyncStatusHandler
{
    private readonly IComponent2020ConnectionProvider _connectionProvider;
    private readonly IComponent2020SyncRunRepository _syncRunRepository;
    private readonly IComponent2020SyncScheduleRepository _syncScheduleRepository;

    public GetComponent2020SyncStatusHandler(
        IComponent2020ConnectionProvider connectionProvider,
        IComponent2020SyncRunRepository syncRunRepository,
        IComponent2020SyncScheduleRepository syncScheduleRepository)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _syncRunRepository = syncRunRepository ?? throw new ArgumentNullException(nameof(syncRunRepository));
        _syncScheduleRepository = syncScheduleRepository ?? throw new ArgumentNullException(nameof(syncScheduleRepository));
    }

    public async Task<GetComponent2020SyncStatusResponse> Handle(GetComponent2020SyncStatusQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken: cancellationToken);
        var isConnected = await _connectionProvider.TestConnectionAsync(connection, cancellationToken);

        var isSchedulerActive = await _syncScheduleRepository.HasActiveSchedulesAsync(cancellationToken);

        var lastSuccessfulRun = await _syncRunRepository.GetLastSuccessfulRunAsync(Component2020SyncScope.All, cancellationToken);

        return new GetComponent2020SyncStatusResponse
        {
            IsConnected = isConnected,
            IsSchedulerActive = isSchedulerActive,
            LastSuccessfulSync = lastSuccessfulRun?.FinishedAt?.DateTime,
            LastSyncStatus = lastSuccessfulRun?.Status
        };
    }
}
