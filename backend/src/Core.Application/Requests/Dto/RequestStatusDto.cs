using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestStatusDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public bool IsFinal { get; init; }

    public bool IsActive { get; init; }

    public string? Description { get; init; }
}
