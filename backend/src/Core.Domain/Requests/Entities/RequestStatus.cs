using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestStatus
{
    public RequestStatusId Id { get; private set; }

    public RequestStatusCode Code { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsFinal { get; private set; }

    public bool IsActive { get; private set; }

    public string? Description { get; private set; }

    private RequestStatus()
    {
        // For EF Core
    }

    public RequestStatus(
        RequestStatusId id,
        RequestStatusCode code,
        string name,
        bool isFinal,
        string? description = null,
        bool isActive = true)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(code.Value))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        Code = code;
        Name = name.Trim();
        IsFinal = isFinal;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
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

    public void MarkFinal()
    {
        IsFinal = true;
    }

    public void MarkNonFinal()
    {
        IsFinal = false;
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
