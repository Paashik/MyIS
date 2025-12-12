using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public readonly record struct RequestLineId
{
    public Guid Value { get; }

    public RequestLineId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("RequestLineId cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static RequestLineId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

