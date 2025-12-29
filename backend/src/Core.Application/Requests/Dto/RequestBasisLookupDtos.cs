using System;

namespace MyIS.Core.Application.Requests.Dto;

public sealed class RequestBasisIncomingRequestLookupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? RequestTypeName { get; init; }
}

public sealed class RequestBasisCustomerOrderLookupDto
{
    public Guid Id { get; init; }
    public string? Number { get; init; }
    public string? CustomerName { get; init; }
}
