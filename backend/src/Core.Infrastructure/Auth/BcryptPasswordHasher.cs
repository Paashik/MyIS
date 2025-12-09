using System;
using MyIS.Core.Application.Auth;

namespace MyIS.Core.Infrastructure.Auth;

public class BcryptPasswordHasher : IPasswordHasher
{
    // BCrypt internally stores salt inside the hash; default work factor (~10-12) is used here.
    public string HashPassword(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyHashedPassword(string hash, string password)
    {
        if (hash is null) throw new ArgumentNullException(nameof(hash));
        if (password is null) throw new ArgumentNullException(nameof(password));

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}