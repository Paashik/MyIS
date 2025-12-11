using System;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class SearchRequestsQuery
{
    /// <summary>
    /// Фильтр по типу заявки (optional).
    /// </summary>
    public Guid? RequestTypeId { get; init; }

    /// <summary>
    /// Фильтр по статусу заявки (optional).
    /// </summary>
    public Guid? RequestStatusId { get; init; }

    /// <summary>
    /// Если true — возвращаются только заявки текущего пользователя.
    /// </summary>
    public bool OnlyMine { get; init; }

    /// <summary>
    /// Текущий пользователь, в том числе для фильтра "Мои".
    /// В WebApi будет браться из контекста аутентификации.
    /// </summary>
    public Guid CurrentUserId { get; init; }

    /// <summary>
    /// Номер страницы (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Результат поиска заявок.
/// </summary>
public sealed class SearchRequestsResult
{
    public PagedResultDto<RequestListItemDto> Page { get; }

    public SearchRequestsResult(PagedResultDto<RequestListItemDto> page)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }
}