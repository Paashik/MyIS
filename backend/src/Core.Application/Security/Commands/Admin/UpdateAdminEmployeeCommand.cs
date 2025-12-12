using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class UpdateAdminEmployeeCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
}

