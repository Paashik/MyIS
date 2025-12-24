using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Mdm.Entities;

public class UnitOfMeasure : IDeactivatable
{
    public Guid Id { get; private set; }

    public string? Code { get; private set; }

    public string Name { get; private set; }

    public string Symbol { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private UnitOfMeasure()
    {
        // For EF Core
    }

    public UnitOfMeasure(string? code, string name)
        : this(code, name, string.Empty)
    {
    }

    public UnitOfMeasure(string? code, string name, string symbol)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = NormalizeOptional(code);
        Name = name.Trim();
        Symbol = (symbol ?? string.Empty).Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string code, string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Code = NormalizeOptional(code);
        Name = name.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string? code, string name, string symbol, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Code = NormalizeOptional(code);
        Name = name.Trim();
        Symbol = (symbol ?? string.Empty).Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
