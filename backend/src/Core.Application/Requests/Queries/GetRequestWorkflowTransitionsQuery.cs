using System;
using System.Collections.Generic;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public sealed class GetRequestWorkflowTransitionsQuery
{
    public Guid CurrentUserId { get; init; }

    public Guid? TypeId { get; init; }
}

public sealed class GetRequestWorkflowTransitionsResult
{
    public IReadOnlyList<RequestWorkflowTransitionDto> Items { get; }

    public GetRequestWorkflowTransitionsResult(IReadOnlyList<RequestWorkflowTransitionDto> items)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}


