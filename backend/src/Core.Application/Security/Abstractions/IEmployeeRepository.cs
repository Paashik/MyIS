using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Application.Security.Abstractions;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Employee>> SearchAsync(string? search, bool? isActive, CancellationToken cancellationToken);

    Task AddAsync(Employee employee, CancellationToken cancellationToken);

    Task UpdateAsync(Employee employee, CancellationToken cancellationToken);
}

