using System;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Mdm.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestLine
{
    public RequestLineId Id { get; private set; }

    public RequestId RequestId { get; private set; }

    public int LineNo { get; private set; }

    // Reference to MDM/Integration (v0.1: no strict FK)
    public ItemId? ItemId { get; private set; }
    public string? ExternalItemCode { get; private set; }
    public string? Description { get; private set; }

    public decimal Quantity { get; private set; }
    public Guid? UnitOfMeasureId { get; private set; }

    public DateTimeOffset? NeedByDate { get; private set; }

    public string? SupplierName { get; private set; }
    public string? SupplierContact { get; private set; }

    public string? ExternalRowReferenceId { get; private set; }

    public Request? Request { get; private set; }

    private RequestLine()
    {
        // For EF Core
    }

    private RequestLine(
        RequestLineId id,
        RequestId requestId,
        int lineNo,
        ItemId? itemId,
        string? externalItemCode,
        string? description,
        decimal quantity,
        Guid? unitOfMeasureId,
        DateTimeOffset? needByDate,
        string? supplierName,
        string? supplierContact,
        string? externalRowReferenceId)
    {
        if (requestId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestId cannot be empty.", nameof(requestId));
        }

        if (lineNo <= 0)
        {
            throw new ArgumentException("LineNo must be positive.", nameof(lineNo));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        }

        Id = id;
        RequestId = requestId;
        LineNo = lineNo;
        ItemId = itemId;
        ExternalItemCode = string.IsNullOrWhiteSpace(externalItemCode) ? null : externalItemCode.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Quantity = quantity;
        UnitOfMeasureId = unitOfMeasureId;
        NeedByDate = needByDate;
        SupplierName = string.IsNullOrWhiteSpace(supplierName) ? null : supplierName.Trim();
        SupplierContact = string.IsNullOrWhiteSpace(supplierContact) ? null : supplierContact.Trim();
        ExternalRowReferenceId = string.IsNullOrWhiteSpace(externalRowReferenceId) ? null : externalRowReferenceId.Trim();
    }

    public static RequestLine Create(
        RequestId requestId,
        int lineNo,
        ItemId? itemId,
        string? externalItemCode,
        string? description,
        decimal quantity,
        Guid? unitOfMeasureId,
        DateTimeOffset? needByDate,
        string? supplierName,
        string? supplierContact,
        string? externalRowReferenceId)
    {
        return new RequestLine(
            RequestLineId.New(),
            requestId,
            lineNo,
            itemId,
            externalItemCode,
            description,
            quantity,
            unitOfMeasureId,
            needByDate,
            supplierName,
            supplierContact,
            externalRowReferenceId);
    }
}

