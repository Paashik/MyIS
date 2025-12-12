using System;
using System.ComponentModel.DataAnnotations;

namespace MyIS.Core.WebApi.Contracts.Requests;

public sealed class RequestLineRequest
{
    [Required]
    public int LineNo { get; init; }

    public Guid? ItemId { get; init; }

    [MaxLength(200)]
    public string? ExternalItemCode { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required]
    public decimal Quantity { get; init; }

    public Guid? UnitOfMeasureId { get; init; }

    public DateTimeOffset? NeedByDate { get; init; }

    [MaxLength(500)]
    public string? SupplierName { get; init; }

    [MaxLength(500)]
    public string? SupplierContact { get; init; }

    [MaxLength(200)]
    public string? ExternalRowReferenceId { get; init; }
}

