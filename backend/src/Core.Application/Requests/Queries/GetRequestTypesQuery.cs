using System;
using System.Collections.Generic;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class GetRequestTypesQuery
{
    /// <summary>
    /// Текущий пользователь, от имени которого выполняется запрос.
    /// На Iteration 1 используется только для AccessChecker.
    /// </summary>
    public Guid CurrentUserId { get; init; }
}

public sealed class GetRequestTypesResult
{
    public IReadOnlyList<RequestTypeDto> Items { get; }

    public GetRequestTypesResult(IReadOnlyList<RequestTypeDto> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}