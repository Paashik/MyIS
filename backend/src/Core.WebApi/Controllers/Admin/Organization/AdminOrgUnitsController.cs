using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi.Contracts.Organization;

namespace MyIS.Core.WebApi.Controllers.Admin.Organization;

[ApiController]
[Route("api/admin/org-units")]
[Authorize(Policy = "Admin.Organization.Edit")]
public sealed class AdminOrgUnitsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminOrgUnitsController(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet]
    public async Task<ActionResult<OrgUnitListItemDto[]>> GetAll(
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OrgUnits.AsNoTracking();

        q = q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.Name.Contains(q) || (x.Code != null && x.Code.Contains(q)));
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new OrgUnitListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                ParentId = x.ParentId,
                ManagerEmployeeId = x.ManagerEmployeeId,
                Phone = x.Phone,
                Email = x.Email,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToArrayAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrgUnitDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OrgUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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

    [HttpPost]
    public async Task<ActionResult<OrgUnitDetailsDto>> Create(
        [FromBody] OrgUnitUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _dbContext.OrgUnits
                .AnyAsync(x => x.Id == request.ParentId.Value, cancellationToken);
            if (!parentExists)
            {
                return BadRequest(new { error = "Parent unit was not found." });
            }
        }

        if (request.ManagerEmployeeId.HasValue)
        {
            var managerExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.ManagerEmployeeId.Value, cancellationToken);
            if (!managerExists)
            {
                return BadRequest(new { error = "Manager employee was not found." });
            }
        }

        var unit = new OrgUnit(
            request.Name,
            request.Code,
            request.ParentId,
            request.ManagerEmployeeId,
            request.Phone,
            request.Email,
            request.IsActive,
            request.SortOrder);

        _dbContext.OrgUnits.Add(unit);

        await ReplaceContactsAsync(unit.Id, request.Contacts, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetById(unit.Id, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrgUnitDetailsDto>> Update(
        Guid id,
        [FromBody] OrgUnitUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        var unit = await _dbContext.OrgUnits
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (unit == null)
        {
            return NotFound();
        }

        if (request.ParentId.HasValue && request.ParentId.Value == id)
        {
            return BadRequest(new { error = "Parent cannot be self." });
        }

        if (request.ParentId.HasValue)
        {
            if (!await _dbContext.OrgUnits.AnyAsync(x => x.Id == request.ParentId.Value, cancellationToken))
            {
                return BadRequest(new { error = "Parent unit was not found." });
            }

            if (await IsDescendantAsync(id, request.ParentId.Value, cancellationToken))
            {
                return BadRequest(new { error = "Parent cannot be a descendant of the unit." });
            }
        }

        if (request.ManagerEmployeeId.HasValue)
        {
            var managerExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.ManagerEmployeeId.Value, cancellationToken);
            if (!managerExists)
            {
                return BadRequest(new { error = "Manager employee was not found." });
            }
        }

        unit.Update(
            request.Name,
            request.Code,
            request.ParentId,
            request.ManagerEmployeeId,
            request.Phone,
            request.Email,
            request.IsActive,
            request.SortOrder);

        await ReplaceContactsAsync(unit.Id, request.Contacts, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetById(unit.Id, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.OrgUnits
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (unit == null)
        {
            return NotFound();
        }

        var hasChildren = await _dbContext.OrgUnits
            .AnyAsync(x => x.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            return BadRequest(new { error = "Cannot delete unit with children." });
        }

        var hasRequests = await _dbContext.Requests
            .AnyAsync(
                r => (r.TargetEntityType == "Department" && r.TargetEntityId == id)
                     || (r.RelatedEntityType == "Department" && r.RelatedEntityId == id),
                cancellationToken);
        if (hasRequests)
        {
            return BadRequest(new { error = "Cannot delete unit referenced by requests." });
        }

        _dbContext.OrgUnits.Remove(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task ReplaceContactsAsync(
        Guid orgUnitId,
        OrgUnitContactRequest[] contacts,
        CancellationToken cancellationToken)
    {
        var incoming = contacts ?? Array.Empty<OrgUnitContactRequest>();
        var ids = incoming.Select(x => x.EmployeeId).Where(x => x != Guid.Empty).Distinct().ToList();
        if (ids.Count > 0)
        {
            var employeesCount = await _dbContext.Employees
                .CountAsync(x => ids.Contains(x.Id), cancellationToken);
            if (employeesCount != ids.Count)
            {
                throw new InvalidOperationException("One or more contact employees were not found.");
            }
        }

        var existing = await _dbContext.OrgUnitContacts
            .Where(x => x.OrgUnitId == orgUnitId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.OrgUnitContacts.RemoveRange(existing);
        }

        foreach (var contact in incoming)
        {
            if (contact.EmployeeId == Guid.Empty)
            {
                continue;
            }

            _dbContext.OrgUnitContacts.Add(new OrgUnitContact(
                orgUnitId,
                contact.EmployeeId,
                contact.IncludeInRequest,
                contact.SortOrder));
        }
    }

    private async Task<bool> IsDescendantAsync(
        Guid unitId,
        Guid newParentId,
        CancellationToken cancellationToken)
    {
        var currentId = newParentId;
        while (true)
        {
            var parent = await _dbContext.OrgUnits
                .AsNoTracking()
                .Where(x => x.Id == currentId)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!parent.HasValue)
            {
                return false;
            }

            if (parent.Value == unitId)
            {
                return true;
            }

            currentId = parent.Value;
        }
    }
}
