using System;

namespace MyIS.Core.Application.Requests.Commands.Admin;

public sealed class CreateAdminRequestStatusCommand
{
    public Guid CurrentUserId { get; init; }

    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsFinal { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

