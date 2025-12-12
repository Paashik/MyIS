using System;

namespace MyIS.Core.Application.Security.Dto;

public sealed class EmployeeDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

