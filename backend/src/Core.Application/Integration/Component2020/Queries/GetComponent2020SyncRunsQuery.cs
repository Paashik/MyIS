using System;
using MyIS.Core.Application.Integration.Component2020.Commands;

namespace MyIS.Core.Application.Integration.Component2020.Queries;

public class GetComponent2020SyncRunsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public string? Status { get; set; }
}

public class GetComponent2020SyncRunsResponse
{
    public Component2020SyncRunDto[] Runs { get; set; } = Array.Empty<Component2020SyncRunDto>();
    public int TotalCount { get; set; }
}

public class Component2020SyncRunDto
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public Guid? StartedByUserId { get; set; }
    public string Scope { get; set; } = null!;
    public string Mode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int ProcessedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? CountersJson { get; set; }
    public string? Summary { get; set; }
}