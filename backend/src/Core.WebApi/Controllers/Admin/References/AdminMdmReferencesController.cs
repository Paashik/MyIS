using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi.Dto.References;

namespace MyIS.Core.WebApi.Controllers.Admin.References;

[ApiController]
[Route("api/admin/references/mdm")]
public sealed class AdminMdmReferencesController : ControllerBase
{
    private static string? NormalizeQuery(string? q)
    {
        q = q?.Trim();
        return string.IsNullOrWhiteSpace(q) ? null : q;
    }

    private static int? TryParseRoleType(string? roleType)
    {
        roleType = NormalizeQuery(roleType);
        if (roleType == null)
        {
            return null;
        }

        if (int.TryParse(roleType, out var numeric))
        {
            return numeric;
        }

        return roleType.ToUpperInvariant() switch
        {
            "SUPPLIER" => CounterpartyRoleTypes.Supplier,
            "CUSTOMER" => CounterpartyRoleTypes.Customer,
            _ => null
        };
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    [HttpGet("units")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetUnits(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.UnitOfMeasures.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                x.Symbol.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmUnitReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("units/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetUnitById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmUnitReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("suppliers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSuppliers(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query =
            from c in dbContext.Counterparties.AsNoTracking()
            join r in dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Supplier && r.IsActive
            select c;

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Inn != null && x.Inn.Contains(q)) ||
                (x.Kpp != null && x.Kpp.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code ?? string.Empty)
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSupplierReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("suppliers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSupplierById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await (
            from c in dbContext.Counterparties.AsNoTracking()
            join r in dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Supplier && r.IsActive && c.Id == id
            select c)
            .Select(x => new MdmSupplierReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("customers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCustomers(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query =
            from c in dbContext.Counterparties.AsNoTracking()
            join r in dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Customer && r.IsActive
            select c;

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Inn != null && x.Inn.Contains(q)) ||
                (x.Kpp != null && x.Kpp.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code ?? string.Empty)
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmCustomerReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("customers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCustomerById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await (
            from c in dbContext.Counterparties.AsNoTracking()
            join r in dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Customer && r.IsActive && c.Id == id
            select c)
            .Select(x => new MdmCustomerReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("counterparties")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCounterparties(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] string? roleType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var parsedRoleType = TryParseRoleType(roleType);

        IQueryable<MyIS.Core.Domain.Mdm.Entities.Counterparty> query = dbContext.Counterparties.AsNoTracking();

        if (parsedRoleType.HasValue)
        {
            query =
                from c in query
                join r in dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
                where r.RoleType == parsedRoleType.Value && r.IsActive
                select c;
        }

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Inn != null && x.Inn.Contains(q)) ||
                (x.Kpp != null && x.Kpp.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code ?? string.Empty)
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmCounterpartyReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                Email = x.Email,
                Phone = x.Phone,
                City = x.City,
                Address = x.Address,
                Site = x.Site,
                SiteLogin = null,
                SitePassword = null,
                Note = x.Note,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var ids = items.Select(x => x.Id).ToList();

        var roles = await dbContext.CounterpartyRoles
            .AsNoTracking()
            .Where(r => ids.Contains(r.CounterpartyId))
            .Select(r => new { r.CounterpartyId, r.RoleType, r.IsActive, r.UpdatedAt })
            .ToListAsync(cancellationToken);

        var links = await dbContext.CounterpartyExternalLinks
            .AsNoTracking()
            .Where(l => ids.Contains(l.CounterpartyId))
            .Select(l => new { l.CounterpartyId, l.ExternalSystem, l.ExternalEntity, l.ExternalId, l.SourceType, l.SyncedAt, l.UpdatedAt })
            .ToListAsync(cancellationToken);

        var rolesById = roles
            .GroupBy(x => x.CounterpartyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new MdmCounterpartyRoleDto
                {
                    RoleType = x.RoleType.ToString(),
                    IsActive = x.IsActive,
                    UpdatedAt = x.UpdatedAt
                }).OrderBy(x => x.RoleType).ToArray());

        var linksById = links
            .GroupBy(x => x.CounterpartyId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new MdmCounterpartyExternalLinkDto
                {
                    ExternalSystem = x.ExternalSystem,
                    ExternalEntity = x.ExternalEntity,
                    ExternalId = x.ExternalId,
                    SourceType = x.SourceType,
                    SyncedAt = x.SyncedAt,
                    UpdatedAt = x.UpdatedAt
                }).OrderBy(x => x.ExternalSystem).ThenBy(x => x.ExternalEntity).ThenBy(x => x.ExternalId).ToArray());

        foreach (var item in items)
        {
            if (rolesById.TryGetValue(item.Id, out var r))
            {
                item.Roles = r;
            }

            if (linksById.TryGetValue(item.Id, out var l))
            {
                item.ExternalLinks = l;
            }
        }

        return Ok(new { total, items });
    }

    [HttpGet("counterparties/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCounterpartyById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmCounterpartyReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                Email = x.Email,
                Phone = x.Phone,
                City = x.City,
                Address = x.Address,
                Site = x.Site,
                SiteLogin = x.SiteLogin,
                SitePassword = x.SitePassword,
                Note = x.Note,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return NotFound();
        }

        entity.Roles = await dbContext.CounterpartyRoles
            .AsNoTracking()
            .Where(r => r.CounterpartyId == id)
            .OrderBy(r => r.RoleType)
            .Select(r => new MdmCounterpartyRoleDto
            {
                RoleType = r.RoleType.ToString(),
                IsActive = r.IsActive,
                UpdatedAt = r.UpdatedAt
            })
            .ToArrayAsync(cancellationToken);

        entity.ExternalLinks = await dbContext.CounterpartyExternalLinks
            .AsNoTracking()
            .Where(l => l.CounterpartyId == id)
            .OrderBy(l => l.ExternalSystem)
            .ThenBy(l => l.ExternalEntity)
            .ThenBy(l => l.ExternalId)
            .Select(l => new MdmCounterpartyExternalLinkDto
            {
                ExternalSystem = l.ExternalSystem,
                ExternalEntity = l.ExternalEntity,
                ExternalId = l.ExternalId,
                SourceType = l.SourceType,
                SyncedAt = l.SyncedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(entity);
    }

    [HttpGet("items")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItems(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.Items
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .AsQueryable();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.Name.Contains(q) ||
                (x.ManufacturerPartNumber != null && x.ManufacturerPartNumber.Contains(q)) ||
                (x.ExternalId != null && x.ExternalId.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmItemReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                ItemKind = x.ItemKind.ToString(),
                IsEskd = x.IsEskd,
                ManufacturerPartNumber = x.ManufacturerPartNumber,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt,
                UnitOfMeasureId = x.UnitOfMeasureId,
                UnitOfMeasureCode = x.UnitOfMeasure != null ? x.UnitOfMeasure.Code : null,
                UnitOfMeasureName = x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : null,
                UnitOfMeasureSymbol = x.UnitOfMeasure != null ? x.UnitOfMeasure.Symbol : null
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("items/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItemById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Items
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x => x.Id == id)
            .Select(x => new MdmItemReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                ItemKind = x.ItemKind.ToString(),
                IsEskd = x.IsEskd,
                ManufacturerPartNumber = x.ManufacturerPartNumber,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt,
                UnitOfMeasureId = x.UnitOfMeasureId,
                UnitOfMeasureCode = x.UnitOfMeasure != null ? x.UnitOfMeasure.Code : null,
                UnitOfMeasureName = x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : null,
                UnitOfMeasureSymbol = x.UnitOfMeasure != null ? x.UnitOfMeasure.Symbol : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("manufacturers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetManufacturers(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.Manufacturers.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.ExternalId != null && x.ExternalId.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code ?? string.Empty)
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmManufacturerReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Site = x.Site,
                Note = x.Note,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("manufacturers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetManufacturerById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Manufacturers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmManufacturerReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                FullName = x.FullName,
                Site = x.Site,
                Note = x.Note,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("body-types")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetBodyTypes(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.BodyTypes.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x => x.Code.Contains(q) || x.Name.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("body-types/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetBodyTypeById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.BodyTypes
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("currencies")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCurrencies(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.Currencies.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code ?? string.Empty)
            .ThenBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmCurrencyReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                Rate = x.Rate,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("currencies/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCurrencyById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Currencies
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmCurrencyReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                Rate = x.Rate,
                IsActive = x.IsActive,
                ExternalSystem = x.ExternalSystem,
                ExternalId = x.ExternalId,
                SyncedAt = x.SyncedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("technical-parameters")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetTechnicalParameters(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.TechnicalParameters.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x => x.Code.Contains(q) || x.Name.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("technical-parameters/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetTechnicalParameterById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.TechnicalParameters
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("parameter-sets")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetParameterSets(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.ParameterSets.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x => x.Code.Contains(q) || x.Name.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("parameter-sets/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetParameterSetById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParameterSets
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("symbols")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSymbols(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var query = dbContext.Symbols.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x => x.Code.Contains(q) || x.Name.Contains(q));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new { total, items });
    }

    [HttpGet("symbols/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSymbolById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Symbols
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmSimpleReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? NotFound() : Ok(entity);
    }
}
