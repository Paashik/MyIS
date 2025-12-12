using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public sealed class GetRequestActionsHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestTransitionRepository _transitionRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestActionsHandler(
        IRequestRepository requestRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _transitionRepository = transitionRepository ?? throw new ArgumentNullException(nameof(transitionRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<string[]> Handle(GetRequestActionsQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.RequestId == Guid.Empty) throw new ArgumentException("RequestId is required.", nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var requestId = new RequestId(query.RequestId);
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{query.RequestId}' was not found.");
        }

        // Просмотр заявки -> просмотр доступных действий
        await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

        var status = await _requestStatusRepository.GetByIdAsync(request.RequestStatusId, cancellationToken);
        if (status is null)
        {
            throw new InvalidOperationException($"RequestStatus with id '{request.RequestStatusId.Value}' was not found.");
        }

        var transitions = await _transitionRepository.GetByTypeAndFromStatusAsync(
            request.RequestTypeId,
            status.Code,
            cancellationToken);

        // Фильтрация по правам (v0.1: AccessChecker может разрешать всё)
        var allowed = new List<string>();
        foreach (var t in transitions)
        {
            try
            {
                await _accessChecker.EnsureCanPerformActionAsync(
                    query.CurrentUserId,
                    request,
                    t.ActionCode,
                    t.RequiredPermission,
                    cancellationToken);

                allowed.Add(t.ActionCode);
            }
            catch
            {
                // ignore
            }
        }

        return allowed
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
    }
}

