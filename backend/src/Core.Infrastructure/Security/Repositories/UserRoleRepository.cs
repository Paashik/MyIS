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

public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly AppDbContext _dbContext;

    public UserRoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) return Array.Empty<Guid>();

        var ids = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        return ids;
    }

    public async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));

        var existing = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.UserRoles.RemoveRange(existing);
        }

        if (roleIds.Count > 0)
        {
            var newEntities = roleIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .Select(roleId => new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedAt = now,
                    CreatedAt = now
                })
                .ToList();

            await _dbContext.UserRoles.AddRangeAsync(newEntities, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

