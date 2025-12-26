using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class SearchRequestsHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestsAccessChecker _accessChecker;
    private readonly IUserRepository _userRepository;

    public SearchRequestsHandler(
        IRequestRepository requestRepository,
        IRequestsAccessChecker accessChecker,
        IUserRepository userRepository)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
        RequestDirection? direction = null;
        if (!string.IsNullOrWhiteSpace(query.Direction))
        {
            if (!Enum.TryParse<RequestDirection>(query.Direction, ignoreCase: true, out var parsed))
            {
                throw new ArgumentException(
                    $"Direction must be one of: {nameof(RequestDirection.Incoming)}|{nameof(RequestDirection.Outgoing)}.",
                    nameof(query));
            }

            direction = parsed;
        }

        var (items, totalCount) = await _requestRepository.SearchAsync(
            requestTypeId: query.RequestTypeId,
            requestStatusId: query.RequestStatusId,
            direction: direction,
            initiatorId: query.OnlyMine ? query.CurrentUserId : null,
            onlyMine: query.OnlyMine,
            pageNumber: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        // 2. Проверка прав на просмотр для каждой заявки (упрощённо)
        var listItems = new List<RequestListItemDto>(items.Count);

        var initiatorIds = items.Select(x => x.InitiatorId).Distinct().ToArray();
        var initiators = await _userRepository.GetByIdsAsync(initiatorIds, cancellationToken);
        var initiatorById = initiators.ToDictionary(
            u => u.Id,
            u =>
            {
                var baseName = u.Employee?.ShortName ?? u.Employee?.FullName ?? u.FullName ?? u.Login;
                return PersonNameFormatter.ToShortName(baseName) ?? baseName;
            });

        foreach (var request in items)
        {
            await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

            initiatorById.TryGetValue(request.InitiatorId, out var initiatorFullName);
            var dto = MapToListItemDto(request, initiatorFullName);
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
            RequestTypeName = type?.Name ?? string.Empty,
            RequestStatusId = status?.Id.Value ?? request.RequestStatusId.Value,
            RequestStatusCode = status?.Code.Value ?? string.Empty,
            RequestStatusName = status?.Name ?? string.Empty,
            InitiatorId = request.InitiatorId,
            InitiatorFullName = initiatorFullName,
            TargetEntityName = request.TargetEntityName,
            RelatedEntityName = request.RelatedEntityName,
            CreatedAt = request.CreatedAt,
            DueDate = request.DueDate
        };
    }
}
