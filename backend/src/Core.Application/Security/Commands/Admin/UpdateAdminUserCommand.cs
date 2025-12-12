using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class UpdateAdminUserCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
    public string? Login { get; init; }
    public bool IsActive { get; init; }
    public Guid? EmployeeId { get; init; }
}

