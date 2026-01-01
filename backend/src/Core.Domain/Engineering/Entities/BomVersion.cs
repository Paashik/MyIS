using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Engineering.Entities;

/// <summary>
/// Версия спецификации (BOM)
/// </summary>
public class BomVersion
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string VersionCode { get; private set; } = null!;
    public BomStatus Status { get; private set; }
    public BomSource Source { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Связи
    public Product Product { get; private set; } = null!;
    private readonly List<BomLine> _lines = new();
    public IReadOnlyCollection<BomLine> Lines => _lines.AsReadOnly();
    private readonly List<BomOperation> _operations = new();
    public IReadOnlyCollection<BomOperation> Operations => _operations.AsReadOnly();

    private BomVersion() { } // EF Core

    public BomVersion(Guid productId, string versionCode, BomSource source)
    {
        ProductId = productId;
        VersionCode = versionCode ?? throw new ArgumentNullException(nameof(versionCode));
        Status = BomStatus.Draft;
        Source = source;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string versionCode, string? description)
    {
        VersionCode = versionCode ?? throw new ArgumentNullException(nameof(versionCode));
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(BomStatus newStatus)
    {
        // Валидация переходов статусов
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}");
        }
        Status = newStatus;
    }

    private bool CanTransitionTo(BomStatus newStatus)
    {
        return (Status, newStatus) switch
        {
            (BomStatus.Draft, BomStatus.Released) => true,
            (BomStatus.Released, BomStatus.Archived) => true,
            (BomStatus.Draft, BomStatus.Archived) => true,
            _ => false
        };
    }
}

public enum BomStatus
{
    Draft,
    Released,
    Archived
}

public enum BomSource
{
    MyIS,
    Component2020,
    Imported
}