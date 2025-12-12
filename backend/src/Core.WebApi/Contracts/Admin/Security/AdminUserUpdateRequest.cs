using System;

namespace MyIS.Core.WebApi.Contracts.Admin.Security;

public sealed class AdminUserUpdateRequest
{
    public string? Login { get; init; }
    public bool IsActive { get; init; }
    public Guid? EmployeeId { get; init; }
}

