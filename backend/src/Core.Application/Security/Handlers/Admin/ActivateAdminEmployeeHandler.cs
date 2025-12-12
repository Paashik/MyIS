using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Commands.Admin;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class ActivateAdminEmployeeHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public ActivateAdminEmployeeHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task Handle(ActivateAdminEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(command));
        if (command.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(command));

        var employee = await _employeeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (employee is null)
        {
            throw new InvalidOperationException($"Employee '{command.Id}' was not found.");
        }

        employee.Activate(DateTimeOffset.UtcNow);
        await _employeeRepository.UpdateAsync(employee, cancellationToken);
    }
}

