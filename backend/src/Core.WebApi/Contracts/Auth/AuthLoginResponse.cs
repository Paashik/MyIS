namespace MyIS.Core.WebApi.Contracts.Auth;

public sealed class AuthLoginResponse
{
    public AuthUserDto User { get; init; } = null!;
}