using System;
using System.Collections.Generic;

namespace MyIS.Core.Application.Auth;

public class AuthResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UserId { get; init; }
    public string? Login { get; init; }
    public string? FullName { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public static AuthResult SuccessResult(Guid userId, string login, string? fullName, IReadOnlyCollection<string>? roles = null)
        => new AuthResult
        {
            Success = true,
            UserId = userId,
            Login = login,
            FullName = fullName,
            Roles = roles ?? Array.Empty<string>()
        };

    public static AuthResult InvalidCredentials(string? message = null)
        => new AuthResult
        {
            Success = false,
            ErrorCode = "INVALID_CREDENTIALS",
            ErrorMessage = message
        };

    public static AuthResult UserInactive(Guid userId, string? login, string? message = null)
        => new AuthResult
        {
            Success = false,
            ErrorCode = "USER_INACTIVE",
            ErrorMessage = message,
            UserId = userId,
            Login = login
        };
}