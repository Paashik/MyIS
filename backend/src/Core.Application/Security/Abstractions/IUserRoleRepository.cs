using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Security.Abstractions;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, DateTimeOffset now, CancellationToken cancellationToken);
}

