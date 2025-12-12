using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Security.Dto;

public sealed class UserDetailsDto
{
    public Guid Id { get; init; }
    public string Login { get; init; } = null!;
    public bool IsActive { get; init; }

    public Guid? EmployeeId { get; init; }
    public string? EmployeeFullName { get; init; }

    public IReadOnlyList<string> RoleCodes { get; init; } = Array.Empty<string>();
}

