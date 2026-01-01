using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Engineering.Entities;

/// <summary>
/// Операция техпроцесса (BOM Operation)
/// </summary>
public class BomOperation
{
    public Guid Id { get; private set; }
    public Guid BomVersionId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? AreaName { get; private set; } // Цех/участок
    public int? DurationMinutes { get; private set; }
    public OperationStatus Status { get; private set; }
    public string? Description { get; private set; }

    // Связи
    public BomVersion BomVersion { get; private set; } = null!;

    private BomOperation() { } // EF Core

    public BomOperation(
        Guid bomVersionId,
        string code,
        string name,
        string? areaName = null,
        int? durationMinutes = null)
    {
        BomVersionId = bomVersionId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        AreaName = areaName;
        DurationMinutes = durationMinutes;
        Status = OperationStatus.Active;
    }

    public void Update(
        string code,
        string name,
        string? areaName = null,
        int? durationMinutes = null,
        string? description = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        AreaName = areaName;
        DurationMinutes = durationMinutes;
        Description = description;
    }

    public void ChangeStatus(OperationStatus status)
    {
        Status = status;
    }
}

public enum OperationStatus
{
    Active,
    Inactive,
    Archived
}