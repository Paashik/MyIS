using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Auth;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<AuthResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        if (login is null) throw new ArgumentNullException(nameof(login));
        if (password is null) throw new ArgumentNullException(nameof(password));

        // For now login comparison is a simple case-sensitive equality.
        // Login comparison policy (case sensitivity, normalization) can be adjusted later if needed.
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Login == login, cancellationToken);

        if (user is null)
        {
            return AuthResult.InvalidCredentials("Invalid login or password.");
        }

        if (!user.IsActive)
        {
            return AuthResult.UserInactive(user.Id, user.Login, "User is inactive.");
        }

        var passwordValid = _passwordHasher.VerifyHashedPassword(user.PasswordHash, password);
        if (!passwordValid)
        {
            return AuthResult.InvalidCredentials("Invalid login or password.");
        }

        var roles = user.UserRoles
            .Select(ur => ur.Role.Code)
            .Where(code => code != null)
            .Distinct()
            .ToArray();

        return AuthResult.SuccessResult(user.Id, user.Login, user.FullName, roles);
    }

    public async Task<AuthResult?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!user.IsActive)
        {
            return AuthResult.UserInactive(user.Id, user.Login, "User is inactive.");
        }

        var roles = user.UserRoles
            .Select(ur => ur.Role.Code)
            .Where(code => code != null)
            .Distinct()
            .ToArray();

        return AuthResult.SuccessResult(user.Id, user.Login, user.FullName, roles);
    }
}