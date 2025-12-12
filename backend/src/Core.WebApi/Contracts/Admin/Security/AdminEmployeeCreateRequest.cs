namespace MyIS.Core.WebApi.Contracts.Admin.Security;

public sealed class AdminEmployeeCreateRequest
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
}

