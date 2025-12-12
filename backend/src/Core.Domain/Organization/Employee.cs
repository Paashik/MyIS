using System;

namespace MyIS.Core.Domain.Organization;

public class Employee
{
    public Guid Id { get; set; }

    public string FullName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    private Employee()
    {
    }

    public static Employee Create(
        Guid id,
        string fullName,
        string? email,
        string? phone,
        string? notes,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required", nameof(fullName));
        }

        return new Employee
        {
            Id = id,
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        string fullName,
        string? email,
        string? phone,
        string? notes,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Employee full name is required", nameof(fullName));
        }

        FullName = fullName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = now;
    }
}

