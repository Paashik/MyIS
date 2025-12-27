using System;
using MyIS.Core.Domain.Common;

namespace MyIS.Core.Domain.Mdm.Entities;

public class BodyType : IDeactivatable
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int? Pins { get; private set; }

    public int? Smt { get; private set; }

    public string? Photo { get; private set; }

    public string? FootPrintPath { get; private set; }

    public string? FootprintRef { get; private set; }

    public string? FootprintRef2 { get; private set; }

    public string? FootPrintRef3 { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private BodyType()
    {
        // For EF Core
    }

    public BodyType(string code, string name, string? description, int? pins, int? smt, string? photo, string? footPrintPath, string? footprintRef, string? footprintRef2, string? footPrintRef3)
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
        Description = description?.Trim();
        Pins = pins;
        Smt = smt;
        Photo = photo?.Trim();
        FootPrintPath = footPrintPath?.Trim();
        FootprintRef = footprintRef?.Trim();
        FootprintRef2 = footprintRef2?.Trim();
        FootPrintRef3 = footPrintRef3?.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? description, int? pins, int? smt, string? photo, string? footPrintPath, string? footprintRef, string? footprintRef2, string? footPrintRef3, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        Pins = pins;
        Smt = smt;
        Photo = photo?.Trim();
        FootPrintPath = footPrintPath?.Trim();
        FootprintRef = footprintRef?.Trim();
        FootprintRef2 = footprintRef2?.Trim();
        FootPrintRef3 = footPrintRef3?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
