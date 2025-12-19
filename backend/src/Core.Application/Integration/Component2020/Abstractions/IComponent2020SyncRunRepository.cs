using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Queries;

namespace MyIS.Core.Application.Integration.Component2020.Abstractions;

public interface IComponent2020SyncRunRepository
{
    Task AddAsync(Component2020SyncRunDto run, CancellationToken cancellationToken);
    Task<GetComponent2020SyncRunsResponse> GetRunsAsync(GetComponent2020SyncRunsQuery query, CancellationToken cancellationToken);
    Task<Component2020SyncRunDto?> GetLastSuccessfulRunAsync(Component2020SyncScope scope, CancellationToken cancellationToken);
    Task<GetComponent2020SyncRunErrorsResponse> GetRunErrorsAsync(GetComponent2020SyncRunErrorsQuery query, CancellationToken cancellationToken);
}