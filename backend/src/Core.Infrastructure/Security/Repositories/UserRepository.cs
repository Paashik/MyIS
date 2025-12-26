using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Users;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Security.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return null;

        return await _dbContext.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login)) return null;

        var normalized = login.Trim();
        return await _dbContext.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Login == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0) return Array.Empty<User>();

        return await _dbContext.Users
            .Include(u => u.Employee)
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(string? search, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (isActive is not null)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                u.Login.Contains(s) ||
                (u.Employee != null && u.Employee.FullName.Contains(s)));
        }

        return await query
            .OrderBy(u => u.Login)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login)) return false;

        var normalized = login.Trim();
        return await _dbContext.Users.AnyAsync(u => u.Login == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByLoginExceptUserAsync(string login, Guid exceptUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login)) return false;

        var normalized = login.Trim();
        return await _dbContext.Users.AnyAsync(u => u.Login == normalized && u.Id != exceptUserId, cancellationToken);
    }

    public async Task<bool> IsEmployeeLinkedToOtherUserAsync(Guid employeeId, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        if (employeeId == Guid.Empty) return false;

        var query = _dbContext.Users.Where(u => u.EmployeeId == employeeId);
        if (exceptUserId is { } ex && ex != Guid.Empty)
        {
            query = query.Where(u => u.Id != ex);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

