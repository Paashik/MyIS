using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public readonly record struct RequestTypeId
{
    public Guid Value { get; }

    public RequestTypeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("RequestTypeId value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static RequestTypeId New() => new(Guid.NewGuid());

    public static RequestTypeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}