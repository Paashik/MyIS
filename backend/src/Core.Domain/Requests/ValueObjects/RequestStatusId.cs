using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public readonly record struct RequestStatusId
{
    public Guid Value { get; }

    public RequestStatusId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("RequestStatusId value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static RequestStatusId New() => new(Guid.NewGuid());

    public static RequestStatusId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}