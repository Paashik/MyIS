using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Domain.Users;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class CreateAdminUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAdminUserHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<UserDetailsDto> Handle(CreateAdminUserCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));

        var login = (command.Login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login)) throw new InvalidOperationException("Login is required.");

        if (await _userRepository.ExistsByLoginAsync(login, cancellationToken))
        {
            throw new InvalidOperationException($"User with login '{login}' already exists.");
        }

        var password = command.Password ?? string.Empty;
        if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Password is required.");

        string? employeeFullName = null;
        if (command.EmployeeId is { } employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) throw new InvalidOperationException($"Employee '{employeeId}' was not found.");

            if (await _userRepository.IsEmployeeLinkedToOtherUserAsync(employeeId, exceptUserId: null, cancellationToken))
            {
                throw new InvalidOperationException("Selected employee is already linked to another user.");
            }

            employeeFullName = employee.FullName;
        }

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = _passwordHasher.HashPassword(password),
            IsActive = command.IsActive,
            EmployeeId = command.EmployeeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);

        return new UserDetailsDto
        {
            Id = user.Id,
            Login = user.Login,
            IsActive = user.IsActive,
            EmployeeId = user.EmployeeId,
            EmployeeFullName = employeeFullName,
            RoleCodes = Array.Empty<string>()
        };
    }
}

