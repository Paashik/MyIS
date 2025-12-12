namespace MyIS.Core.WebApi.Contracts.Admin.Security;

public sealed class AdminUserResetPasswordRequest
{
    public string? NewPassword { get; init; }
}

