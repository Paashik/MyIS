namespace MyIS.Core.Application.Auth;

/// <summary>
/// Provides password hashing and verification using a strong one-way algorithm.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Creates a hash for the specified plain text password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that the password matches the previously generated hash.
    /// </summary>
    bool VerifyHashedPassword(string hash, string password);
}