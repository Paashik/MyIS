using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi.Contracts.Organization;

namespace MyIS.Core.WebApi.Controllers.Organization;

[ApiController]
[Route("api/org-units")]
[Authorize]
public sealed class OrgUnitsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public OrgUnitsController(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrgUnitDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OrgUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (entity == null)
        {
            return NotFound();
        }

        var contacts = await (
            from c in _dbContext.OrgUnitContacts.AsNoTracking()
            join e in _dbContext.Employees.AsNoTracking() on c.EmployeeId equals e.Id
            where c.OrgUnitId == id
            orderby c.SortOrder, e.FullName
            select new OrgUnitContactDto
            {
                EmployeeId = e.Id,
                EmployeeFullName = e.FullName,
                EmployeeEmail = e.Email,
                EmployeePhone = e.Phone,
                IncludeInRequest = c.IncludeInRequest,
                SortOrder = c.SortOrder
            }).ToArrayAsync(cancellationToken);

        return Ok(new OrgUnitDetailsDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            ParentId = entity.ParentId,
            ManagerEmployeeId = entity.ManagerEmployeeId,
            Phone = entity.Phone,
            Email = entity.Email,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder,
            Contacts = contacts
        });
    }
}
