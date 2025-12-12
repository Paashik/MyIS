namespace MyIS.Core.WebApi.Contracts.Admin.Security;

public sealed class AdminRoleCreateRequest
{
    public string? Code { get; init; }
    public string? Name { get; init; }
}

