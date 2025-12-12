using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestType
{
    public RequestTypeId Id { get; private set; }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public RequestDirection Direction { get; private set; }

    public bool IsActive { get; private set; }

    private RequestType()
    {
        // For EF Core
    }

    public RequestType(
        RequestTypeId id,
        string code,
        string name,
        RequestDirection direction,
        string? description = null,
        bool isActive = true)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        Code = code.Trim();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Direction = direction;
        IsActive = isActive;
    }

    public void ChangeDirection(RequestDirection direction)
    {
        Direction = direction;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
}

        Name = name.Trim();
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
