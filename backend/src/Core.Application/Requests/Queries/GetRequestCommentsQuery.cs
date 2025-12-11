using System;
using System.Collections.Generic;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class GetRequestCommentsQuery
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

public sealed class GetRequestCommentsResult
{
    public IReadOnlyList<RequestCommentDto> Items { get; }

    public GetRequestCommentsResult(IReadOnlyList<RequestCommentDto> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}