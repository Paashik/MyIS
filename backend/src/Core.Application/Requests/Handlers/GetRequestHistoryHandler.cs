using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestHistoryHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestHistoryRepository _historyRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestHistoryHandler(
        IRequestRepository requestRepository,
        IRequestHistoryRepository historyRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<GetRequestHistoryResult> Handle(
        GetRequestHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        if (query.RequestId == Guid.Empty)
        {
            throw new ArgumentException("RequestId is required.", nameof(query));
        }

        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        var requestId = new RequestId(query.RequestId);

        // 1. Убеждаемся, что заявка существует
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{query.RequestId}' was not found.");
        }

        // 2. Проверка прав на просмотр истории (те же права, что и на просмотр заявки)
        await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

        // 3. Загрузка истории
        var items = await _historyRepository.GetByRequestIdAsync(requestId, cancellationToken);

        // 4. Маппинг в DTO
        var dtos = new List<RequestHistoryItemDto>(items.Count);
        foreach (var h in items)
        {
            var dto = new RequestHistoryItemDto
            {
                Id = h.Id,
                RequestId = h.RequestId.Value,
                Action = h.Action,
                PerformedBy = h.PerformedBy,
                PerformedByFullName = null,
                Timestamp = h.Timestamp,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                Comment = h.Comment
            };

            dtos.Add(dto);
        }

        return new GetRequestHistoryResult(dtos);
    }
}