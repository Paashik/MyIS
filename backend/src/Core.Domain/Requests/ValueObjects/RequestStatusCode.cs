using System;

namespace MyIS.Core.Domain.Requests.ValueObjects;

public readonly record struct RequestStatusCode
{
    public string Value { get; }

    public RequestStatusCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RequestStatusCode value cannot be null or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public static RequestStatusCode Draft => new("Draft");
    public static RequestStatusCode Submitted => new("Submitted");
    public static RequestStatusCode InReview => new("InReview");
    public static RequestStatusCode Approved => new("Approved");
    public static RequestStatusCode Rejected => new("Rejected");
    public static RequestStatusCode InWork => new("InWork");
    public static RequestStatusCode Done => new("Done");
    public static RequestStatusCode Closed => new("Closed");

    public override string ToString() => Value;
}