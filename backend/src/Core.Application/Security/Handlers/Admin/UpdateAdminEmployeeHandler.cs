using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class UpdateAdminEmployeeHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateAdminEmployeeHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<EmployeeDto> Handle(UpdateAdminEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var employee = await _employeeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (employee is null)
        {
            throw new InvalidOperationException($"Employee '{command.Id}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        employee.Update(
            fullName: command.FullName ?? string.Empty,
            email: command.Email,
            phone: command.Phone,
            notes: command.Notes,
            now: now);

        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return new EmployeeDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Email = employee.Email,
            Phone = employee.Phone,
            Notes = employee.Notes,
            IsActive = employee.IsActive
        };
    }
}

