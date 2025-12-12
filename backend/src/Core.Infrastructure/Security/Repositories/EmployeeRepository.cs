using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Security.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _dbContext;

    public EmployeeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) return null;

        return await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> SearchAsync(string? search, bool? isActive, CancellationToken cancellationToken)
    {
        var query = _dbContext.Employees.AsQueryable();

        if (isActive is not null)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(e =>
                e.FullName.Contains(s) ||
                (e.Email != null && e.Email.Contains(s)) ||
                (e.Phone != null && e.Phone.Contains(s)));
        }

        return await query
            .OrderBy(e => e.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (employee is null) throw new ArgumentNullException(nameof(employee));

        await _dbContext.Employees.AddAsync(employee, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (employee is null) throw new ArgumentNullException(nameof(employee));

        _dbContext.Employees.Update(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

