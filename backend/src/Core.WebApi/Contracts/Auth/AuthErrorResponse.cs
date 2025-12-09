namespace MyIS.Core.WebApi.Contracts.Auth;

public sealed class AuthErrorResponse
{
    public string Code { get; init; } = null!;
    public string Message { get; init; } = null!;
    public object? Details { get; init; }
}