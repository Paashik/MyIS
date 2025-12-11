using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;

namespace MyIS.Core.Application.Requests.Handlers;

public class SearchRequestsHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public SearchRequestsHandler(
        IRequestRepository requestRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<SearchRequestsResult> Handle(
        SearchRequestsQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        // 1. Загрузка данных из репозитория
        var (items, totalCount) = await _requestRepository.SearchAsync(
            requestTypeId: query.RequestTypeId,
            requestStatusId: query.RequestStatusId,
            initiatorId: query.OnlyMine ? query.CurrentUserId : null,
            onlyMine: query.OnlyMine,
            pageNumber: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        // 2. Проверка прав на просмотр для каждой заявки (упрощённо)
        var listItems = new List<RequestListItemDto>(items.Count);

        foreach (var request in items)
        {
            await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

            var dto = MapToListItemDto(request, initiatorFullName: null);
            listItems.Add(dto);
        }

        var page = new PagedResultDto<RequestListItemDto>(
            listItems,
            totalCount,
            pageNumber,
            pageSize);

        return new SearchRequestsResult(page);
    }

    private static RequestListItemDto MapToListItemDto(Request request, string? initiatorFullName)
    {
        // На Iteration 1 полагаемся на то, что репозиторий может подгрузить навигационные свойства.
        // Если Type/Status не загружены, используем безопасные значения по умолчанию.
        var type = request.Type;
        var status = request.Status;

        return new RequestListItemDto
        {
            Id = request.Id.Value,
            Title = request.Title,
            RequestTypeId = type?.Id.Value ?? request.RequestTypeId.Value,
            RequestTypeCode = type?.Code ?? string.Empty,
            RequestTypeName = type?.Name ?? string.Empty,
            RequestStatusId = status?.Id.Value ?? request.RequestStatusId.Value,
            RequestStatusCode = status?.Code.Value ?? string.Empty,
            RequestStatusName = status?.Name ?? string.Empty,
            InitiatorId = request.InitiatorId,
            InitiatorFullName = initiatorFullName,
            CreatedAt = request.CreatedAt,
            DueDate = request.DueDate
        };
    }
}