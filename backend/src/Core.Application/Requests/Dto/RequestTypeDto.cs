using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestTypeDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>
    /// "Incoming" | "Outgoing".
    /// </summary>
    public string Direction { get; init; } = null!;

    public string? Description { get; init; }

    public bool IsActive { get; init; }
}
