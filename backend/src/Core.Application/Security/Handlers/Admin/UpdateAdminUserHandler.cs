using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class UpdateAdminUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateAdminUserHandler(IUserRepository userRepository, IEmployeeRepository employeeRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<UserDetailsDto> Handle(UpdateAdminUserCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{command.Id}' was not found.");
        }

        var login = (command.Login ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login)) throw new InvalidOperationException("Login is required.");

        if (await _userRepository.ExistsByLoginExceptUserAsync(login, user.Id, cancellationToken))
        {
            throw new InvalidOperationException($"User with login '{login}' already exists.");
        }

        string? employeeFullName = null;
        if (command.EmployeeId is { } employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) throw new InvalidOperationException($"Employee '{employeeId}' was not found.");

            if (await _userRepository.IsEmployeeLinkedToOtherUserAsync(employeeId, exceptUserId: user.Id, cancellationToken))
            {
                throw new InvalidOperationException("Selected employee is already linked to another user.");
            }

            employeeFullName = employee.FullName;
        }

        user.UpdateDetails(
            login: login,
            isActive: command.IsActive,
            employeeId: command.EmployeeId,
            now: DateTimeOffset.UtcNow);

        await _userRepository.UpdateAsync(user, cancellationToken);

        var roleCodes = user.UserRoles
            .Select(ur => ur.Role.Code)
            .OrderBy(x => x)
            .ToArray();

        return new UserDetailsDto
        {
            Id = user.Id,
            Login = user.Login,
            IsActive = user.IsActive,
            EmployeeId = user.EmployeeId,
            EmployeeFullName = employeeFullName,
            RoleCodes = roleCodes
        };
    }
}

