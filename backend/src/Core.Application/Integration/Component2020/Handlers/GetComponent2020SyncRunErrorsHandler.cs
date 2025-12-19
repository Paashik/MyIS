using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Queries;

namespace MyIS.Core.Application.Integration.Component2020.Handlers;

public class GetComponent2020SyncRunErrorsHandler
{
    private readonly IComponent2020SyncRunRepository _syncRunRepository;

    public GetComponent2020SyncRunErrorsHandler(IComponent2020SyncRunRepository syncRunRepository)
    {
        _syncRunRepository = syncRunRepository ?? throw new ArgumentNullException(nameof(syncRunRepository));
    }

    public async Task<GetComponent2020SyncRunErrorsResponse> Handle(GetComponent2020SyncRunErrorsQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        return await _syncRunRepository.GetRunErrorsAsync(query, cancellationToken);
    }
}