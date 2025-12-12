using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class DeactivateAdminUserHandler
{
    private readonly IUserRepository _userRepository;

    public DeactivateAdminUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task Handle(DeactivateAdminUserCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) throw new InvalidOperationException($"User '{command.Id}' was not found.");

        if (user.IsActive)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}

