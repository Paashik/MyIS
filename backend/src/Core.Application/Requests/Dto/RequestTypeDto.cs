using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestTypeDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; }
}