using System;

namespace MyIS.Core.Application.Requests.Commands.Admin;

public sealed class ArchiveAdminRequestTypeCommand
{
    public Guid CurrentUserId { get; init; }

    public Guid Id { get; init; }
}

