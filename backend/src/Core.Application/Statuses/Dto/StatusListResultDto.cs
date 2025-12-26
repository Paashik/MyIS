using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Statuses.Dto;

public sealed class StatusListResultDto<T>
{
    public int Total { get; init; }
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}
