using System;

namespace MyIS.Core.Application.Requests.Dto;

public sealed class RequestCounterpartyLookupDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? FullName { get; init; }
}
