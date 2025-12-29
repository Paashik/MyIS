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

        // 1. Р—Р°РіСЂСѓР·РєР° РґР°РЅРЅС‹С… РёР· СЂРµРїРѕР·РёС‚РѕСЂРёСЏ
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
            managerId: query.OnlyMine ? query.CurrentUserId : null,
            onlyMine: query.OnlyMine,
            pageNumber: pageNumber,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        // 2. РџСЂРѕРІРµСЂРєР° РїСЂР°РІ РЅР° РїСЂРѕСЃРјРѕС‚СЂ РґР»СЏ РєР°Р¶РґРѕР№ Р·Р°СЏРІРєРё (СѓРїСЂРѕС‰С‘РЅРЅРѕ)
        var listItems = new List<RequestListItemDto>(items.Count);

        var managerIds = items.Select(x => x.ManagerId).Distinct().ToArray();
        var managers = await _userRepository.GetByIdsAsync(managerIds, cancellationToken);
        var managerById = managers.ToDictionary(
            u => u.Id,
            u =>
            {
                var baseName = u.Employee?.ShortName ?? u.Employee?.FullName ?? u.FullName ?? u.Login;
                return PersonNameFormatter.ToShortName(baseName) ?? baseName;
            });

        foreach (var request in items)
        {
            await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

            managerById.TryGetValue(request.ManagerId, out var managerFullName);
            var dto = MapToListItemDto(request, managerFullName);
            listItems.Add(dto);
        }

        var page = new PagedResultDto<RequestListItemDto>(
            listItems,
            totalCount,
            pageNumber,
            pageSize);

        return new SearchRequestsResult(page);
    }

    private static RequestListItemDto MapToListItemDto(Request request, string? managerFullName)
    {
        // РќР° Iteration 1 РїРѕР»Р°РіР°РµРјСЃСЏ РЅР° С‚Рѕ, С‡С‚Рѕ СЂРµРїРѕР·РёС‚РѕСЂРёР№ РјРѕР¶РµС‚ РїРѕРґРіСЂСѓР·РёС‚СЊ РЅР°РІРёРіР°С†РёРѕРЅРЅС‹Рµ СЃРІРѕР№СЃС‚РІР°.
        // Р•СЃР»Рё Type/Status РЅРµ Р·Р°РіСЂСѓР¶РµРЅС‹, РёСЃРїРѕР»СЊР·СѓРµРј Р±РµР·РѕРїР°СЃРЅС‹Рµ Р·РЅР°С‡РµРЅРёСЏ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ.
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
            ManagerId = request.ManagerId,
            ManagerFullName = managerFullName,
            TargetEntityName = request.TargetEntityName,
            RelatedEntityName = request.RelatedEntityName,
            Description = request.Description,
            BasisType = request.BasisType,
            BasisRequestId = request.BasisRequestId,
            BasisCustomerOrderId = request.BasisCustomerOrderId,
            BasisDescription = request.BasisDescription,
            CreatedAt = request.CreatedAt,
            DueDate = request.DueDate
        };
    }
}





