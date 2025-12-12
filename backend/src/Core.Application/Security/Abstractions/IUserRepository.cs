using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> SearchAsync(string? search, bool? isActive, CancellationToken cancellationToken);

    Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken);

    Task<bool> ExistsByLoginExceptUserAsync(string login, Guid exceptUserId, CancellationToken cancellationToken);

    Task<bool> IsEmployeeLinkedToOtherUserAsync(Guid employeeId, Guid? exceptUserId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}

