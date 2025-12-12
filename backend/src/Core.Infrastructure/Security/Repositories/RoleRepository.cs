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

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return null;

        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Roles
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var normalized = code.Trim();
        return await _dbContext.Roles.AnyAsync(r => r.Code == normalized, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        if (role is null) throw new ArgumentNullException(nameof(role));

        await _dbContext.Roles.AddAsync(role, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        if (role is null) throw new ArgumentNullException(nameof(role));

        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

