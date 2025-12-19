using System;

namespace MyIS.Core.Application.Integration.Component2020.Queries;

public class GetComponent2020SyncRunErrorsQuery
{
    public Guid RunId { get; set; }
}

public class GetComponent2020SyncRunErrorsResponse
{
    public Component2020SyncErrorDto[] Errors { get; set; } = Array.Empty<Component2020SyncErrorDto>();
}

public class Component2020SyncErrorDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string? ExternalEntity { get; set; }
    public string? ExternalKey { get; set; }
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}