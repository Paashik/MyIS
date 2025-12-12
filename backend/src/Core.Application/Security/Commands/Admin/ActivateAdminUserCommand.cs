using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class ActivateAdminUserCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
}

