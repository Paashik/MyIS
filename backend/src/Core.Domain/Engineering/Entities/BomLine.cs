using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Engineering.Entities;

/// <summary>
/// Строка спецификации (BOM Line)
/// </summary>
public class BomLine
{
    public Guid Id { get; private set; }
    public Guid BomVersionId { get; private set; }
    public Guid ParentItemId { get; private set; } // Узел, к которому относится строка
    public Guid ItemId { get; private set; } // Номенклатура из MDM
    public BomRole Role { get; private set; }
    public decimal Quantity { get; private set; }
    public string? UnitOfMeasure { get; private set; }
    public string? PositionNo { get; private set; }
    public string? Notes { get; private set; }
    public LineStatus Status { get; private set; }

    // Связи
    public BomVersion BomVersion { get; private set; } = null!;

    private BomLine() { } // EF Core

    public BomLine(
        Guid bomVersionId,
        Guid parentItemId,
        Guid itemId,
        BomRole role,
        decimal quantity,
        string? unitOfMeasure = null)
    {
        BomVersionId = bomVersionId;
        ParentItemId = parentItemId;
        ItemId = itemId;
        Role = role;
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be positive", nameof(quantity));
        UnitOfMeasure = unitOfMeasure;
        Status = LineStatus.Valid;
    }

    public void Update(
        BomRole? role = null,
        decimal? quantity = null,
        string? positionNo = null,
        string? notes = null,
        Guid? itemId = null)
    {
        if (role.HasValue) Role = role.Value;
        if (quantity.HasValue)
        {
            Quantity = quantity.Value > 0 ? quantity.Value : throw new ArgumentException("Quantity must be positive", nameof(quantity));
        }
        if (positionNo != null) PositionNo = positionNo;
        if (notes != null) Notes = notes;
        if (itemId.HasValue) ItemId = itemId.Value;
    }

    public void UpdateStatus(LineStatus status)
    {
        Status = status;
    }
}

public enum BomRole
{
    Component,
    Material,
    SubAssembly,
    Service
}

public enum LineStatus
{
    Valid,
    Warning,
    Error,
    Archived
}