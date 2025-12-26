using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminEmployeesHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetAdminEmployeesHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<IReadOnlyList<EmployeeDto>> Handle(GetAdminEmployeesQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var items = await _employeeRepository.SearchAsync(query.Search, query.IsActive, cancellationToken);

        var dtos = new List<EmployeeDto>(items.Count);
        foreach (var e in items)
        {
            dtos.Add(Map(e));
        }

        return dtos;
    }

    private static EmployeeDto Map(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            FullName = e.FullName,
            ShortName = e.ShortName,
            Email = e.Email,
            Phone = e.Phone,
            Notes = e.Notes,
            IsActive = e.IsActive
        };
    }
}

