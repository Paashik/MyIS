using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class CreateAdminUserCommand
{
    public Guid CurrentUserId { get; init; }
    public string? Login { get; init; }
    public string? Password { get; init; }
    public bool IsActive { get; init; }
    public Guid? EmployeeId { get; init; }
}

