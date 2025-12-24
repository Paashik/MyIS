using System;
using System.Collections.Generic;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Domain.Users;

public class User
{
    public Guid Id { get; private set; }

    public string Login { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string? FullName { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? EmployeeId { get; private set; }

    public Employee? Employee { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private User()
    {
        // For EF Core
    }

    private User(
        Guid id,
        string login,
        string passwordHash,
        bool isActive,
        Guid? employeeId,
        string? fullName,
        DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));

        login = (login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ArgumentException("Login is required.", nameof(login));
        }

        passwordHash = (passwordHash ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId cannot be empty.", nameof(employeeId));
        }

        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        IsActive = isActive;
        EmployeeId = employeeId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static User Create(
        Guid id,
        string login,
        string passwordHash,
        bool isActive,
        Guid? employeeId,
        DateTimeOffset now,
        string? fullName = null)
    {
        return new User(
            id: id,
            login: login,
            passwordHash: passwordHash,
            isActive: isActive,
            employeeId: employeeId,
            fullName: fullName,
            now: now);
    }

    public void UpdateDetails(
        string login,
        bool isActive,
        Guid? employeeId,
        DateTimeOffset now,
        string? fullName = null)
    {
        login = (login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ArgumentException("Login is required.", nameof(login));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("EmployeeId cannot be empty.", nameof(employeeId));
        }

        Login = login;
        IsActive = isActive;
        EmployeeId = employeeId;
        FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
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

    public void ResetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        passwordHash = (passwordHash ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        UpdatedAt = now;
    }
}
