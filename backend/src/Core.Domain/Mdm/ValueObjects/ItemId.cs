using System;

namespace MyIS.Core.Domain.Mdm.ValueObjects;

public readonly record struct ItemId
{
    public Guid Value { get; }

    public ItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ItemId value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static ItemId New() => new(Guid.NewGuid());

    public static ItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}