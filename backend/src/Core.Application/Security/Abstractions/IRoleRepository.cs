using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Abstractions;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(Role role, CancellationToken cancellationToken);

    Task UpdateAsync(Role role, CancellationToken cancellationToken);
}

