namespace MyIS.Core.WebApi.Contracts.Auth;

public sealed class AuthLoginRequest
{
    public string Login { get; init; } = null!;
    public string Password { get; init; } = null!;
}