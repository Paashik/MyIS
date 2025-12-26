using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class CreateAdminEmployeeHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public CreateAdminEmployeeHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<EmployeeDto> Handle(CreateAdminEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));

        var now = DateTimeOffset.UtcNow;
        var employee = Employee.Create(
            id: Guid.NewGuid(),
            fullName: command.FullName ?? string.Empty,
            email: command.Email,
            phone: command.Phone,
            notes: command.Notes,
            now: now);

        await _employeeRepository.AddAsync(employee, cancellationToken);

        return new EmployeeDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            ShortName = employee.ShortName,
            Email = employee.Email,
            Phone = employee.Phone,
            Notes = employee.Notes,
            IsActive = employee.IsActive
        };
    }
}

