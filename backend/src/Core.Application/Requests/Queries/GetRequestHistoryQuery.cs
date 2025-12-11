using System;
using System.Collections.Generic;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class GetRequestHistoryQuery
{
    /// <summary>
    /// Идентификатор заявки.
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Текущий пользователь, от имени которого выполняется запрос.
    /// Используется AccessChecker-ом.
    /// </summary>
    public Guid CurrentUserId { get; init; }
}

public sealed class GetRequestHistoryResult
{
    public IReadOnlyList<RequestHistoryItemDto> Items { get; }

    public GetRequestHistoryResult(IReadOnlyList<RequestHistoryItemDto> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}