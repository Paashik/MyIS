using System;

namespace MyIS.Core.Application.Requests.Dto;

/// <summary>
/// Ввод строк заявки (v0.1: replace-all стратегия).
/// </summary>
public sealed class RequestLineInputDto
{
    public int LineNo { get; init; }

    public Guid? ItemId { get; init; }

    public string? ExternalItemCode { get; init; }

    public string? Description { get; init; }

    public decimal Quantity { get; init; }

    public Guid? UnitOfMeasureId { get; init; }

    public DateTimeOffset? NeedByDate { get; init; }

    public string? SupplierName { get; init; }

    public string? SupplierContact { get; init; }

    public string? ExternalRowReferenceId { get; init; }
}

