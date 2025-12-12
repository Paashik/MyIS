using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.Domain.Organization;

namespace MyIS.Core.Application.Security.Handlers.Admin;

public sealed class GetAdminEmployeeByIdHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetAdminEmployeeByIdHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    public async Task<EmployeeDto> Handle(GetAdminEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));
        if (query.Id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(query));

        var e = await _employeeRepository.GetByIdAsync(query.Id, cancellationToken);
        if (e is null)
        {
            throw new InvalidOperationException($"Employee '{query.Id}' was not found.");
        }

        return Map(e);
    }

    private static EmployeeDto Map(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            FullName = e.FullName,
            Email = e.Email,
            Phone = e.Phone,
            Notes = e.Notes,
            IsActive = e.IsActive
        };
    }
}

