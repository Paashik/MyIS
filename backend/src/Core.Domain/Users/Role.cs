using System;
using System.Collections.Generic;

namespace MyIS.Core.Domain.Users;

public class Role
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role()
    {
        // For EF Core
    }

    private Role(Guid id, string code, string name, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));

        code = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        Code = code;
        Name = name;
        CreatedAt = createdAt;
    }

    public static Role Create(Guid id, string code, string name, DateTimeOffset createdAt)
    {
        return new Role(id, code, name, createdAt);
    }

    public void Rename(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = name;
    }
}
