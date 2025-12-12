using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class UpdateAdminRoleCommand
{
    public Guid CurrentUserId { get; init; }
    public Guid Id { get; init; }
    public string? Name { get; init; }
}

