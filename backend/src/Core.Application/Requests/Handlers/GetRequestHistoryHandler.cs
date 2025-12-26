using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestHistoryHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestHistoryRepository _historyRepository;
    private readonly IRequestsAccessChecker _accessChecker;
    private readonly IUserRepository _userRepository;

    public GetRequestHistoryHandler(
        IRequestRepository requestRepository,
        IRequestHistoryRepository historyRepository,
        IRequestsAccessChecker accessChecker,
        IUserRepository userRepository)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
        var performerIds = items.Select(x => x.PerformedBy).Distinct().ToArray();
        var performers = await _userRepository.GetByIdsAsync(performerIds, cancellationToken);
        var performerById = performers.ToDictionary(
            u => u.Id,
            u =>
            {
                var baseName = u.Employee?.ShortName ?? u.Employee?.FullName ?? u.FullName ?? u.Login;
                return PersonNameFormatter.ToShortName(baseName) ?? baseName;
            });

        foreach (var h in items)
        {
            performerById.TryGetValue(h.PerformedBy, out var performerFullName);
            var dto = new RequestHistoryItemDto
            {
                Id = h.Id,
                RequestId = h.RequestId.Value,
                Action = h.Action,
                PerformedBy = h.PerformedBy,
                PerformedByFullName = performerFullName,
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
