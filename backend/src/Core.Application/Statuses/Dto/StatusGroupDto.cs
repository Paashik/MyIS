using System;

namespace MyIS.Core.Application.Statuses.Dto;

public sealed class StatusGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsRequestsGroup { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
