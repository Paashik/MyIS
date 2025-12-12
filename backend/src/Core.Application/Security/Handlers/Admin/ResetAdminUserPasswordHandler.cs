using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class ResetAdminUserPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetAdminUserPasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task Handle(ResetAdminUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var password = command.NewPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("NewPassword is required.");

        var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) throw new InvalidOperationException($"User '{command.Id}' was not found.");

        user.PasswordHash = _passwordHasher.HashPassword(password);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}

