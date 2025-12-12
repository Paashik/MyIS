using System;
using System.Collections.Generic;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Domain.Users;

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
