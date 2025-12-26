using System;

namespace MyIS.Core.Application.Statuses.Dto;

public sealed class StatusDto
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string? GroupName { get; init; }
    public string Name { get; init; } = string.Empty;
    public int? Color { get; init; }
    public int? Flags { get; init; }
    public int? SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
