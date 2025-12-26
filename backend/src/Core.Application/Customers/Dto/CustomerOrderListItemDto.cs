using System;

namespace MyIS.Core.Application.Customers.Dto;

public sealed class CustomerOrderListItemDto
{
    public Guid Id { get; init; }
    public string? Number { get; init; }
    public DateTime? OrderDate { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public int? State { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public Guid? PersonId { get; init; }
    public string? PersonName { get; init; }
    public string? Contract { get; init; }
    public string? Note { get; init; }
    public string? StatusName { get; init; }
    public int? StatusColor { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
