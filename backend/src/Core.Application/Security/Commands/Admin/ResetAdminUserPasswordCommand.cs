using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class ResetAdminUserPasswordCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
    public string? NewPassword { get; init; }
}

