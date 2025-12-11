using System;
using System.Collections.Generic;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class GetRequestStatusesQuery
{
    /// <summary>
    /// Текущий пользователь, от имени которого выполняется запрос.
    /// На Iteration 1 используется только для AccessChecker.
    /// </summary>
    public Guid CurrentUserId { get; init; }
}

public sealed class GetRequestStatusesResult
{
    public IReadOnlyList<RequestStatusDto> Items { get; }

    public GetRequestStatusesResult(IReadOnlyList<RequestStatusDto> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}