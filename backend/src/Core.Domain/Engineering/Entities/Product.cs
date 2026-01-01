using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Engineering.Entities;

/// <summary>
/// Изделие в инженерном домене
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ProductType Type { get; private set; }
    public Guid ItemId { get; private set; } // Ссылка на MDM Item
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Связи
    private readonly List<BomVersion> _bomVersions = new();
    public IReadOnlyCollection<BomVersion> BomVersions => _bomVersions.AsReadOnly();

    private Product() { } // EF Core

    public Product(string code, string name, ProductType type, Guid itemId)
    {
        Id = Guid.NewGuid();
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        ItemId = itemId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum ProductType
{
    Assembly,
    Part,
    Component,
    Material
}