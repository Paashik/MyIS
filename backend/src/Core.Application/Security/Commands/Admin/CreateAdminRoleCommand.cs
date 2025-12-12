using System;

namespace MyIS.Core.Application.Security.Commands.Admin;

public sealed class CreateAdminRoleCommand
{
    public Guid CurrentUserId { get; init; }
    public string? Code { get; init; }
    public string? Name { get; init; }
}

