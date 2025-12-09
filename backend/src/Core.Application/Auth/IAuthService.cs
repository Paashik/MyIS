using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Auth;

/// <summary>
/// Provides application-level authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate user by login and password.
    /// </summary>
    Task<AuthResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user info and roles by identifier.
    /// </summary>
    Task<AuthResult?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}