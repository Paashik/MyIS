using System;

namespace MyIS.Core.Application.Requests.Commands.Admin;

public sealed class UpdateAdminRequestTypeCommand
{
    public Guid CurrentUserId { get; init; }

    public Guid Id { get; init; }

    public string Name { get; init; } = null!;
    public string Direction { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

