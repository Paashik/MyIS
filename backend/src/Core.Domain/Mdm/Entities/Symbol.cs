using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Symbol : IDeactivatable
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? SymbolValue { get; private set; }

    public string? Photo { get; private set; }

    public string? LibraryPath { get; private set; }

    public string? LibraryRef { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Symbol()
    {
        // For EF Core
    }

    public Symbol(string code, string name, string? symbolValue, string? photo, string? libraryPath, string? libraryRef)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        SymbolValue = symbolValue?.Trim();
        Photo = photo?.Trim();
        LibraryPath = libraryPath?.Trim();
        LibraryRef = libraryRef?.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? symbolValue, string? photo, string? libraryPath, string? libraryRef, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        SymbolValue = symbolValue?.Trim();
        Photo = photo?.Trim();
        LibraryPath = libraryPath?.Trim();
        LibraryRef = libraryRef?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
