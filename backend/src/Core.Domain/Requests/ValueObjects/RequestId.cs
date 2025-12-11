using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public readonly record struct RequestId
{
    public Guid Value { get; }

    public RequestId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("RequestId value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static RequestId New() => new(Guid.NewGuid());

    public static RequestId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}