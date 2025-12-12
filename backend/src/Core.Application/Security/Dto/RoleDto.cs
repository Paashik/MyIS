using System;

namespace MyIS.Core.Application.Security.Dto;

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}

