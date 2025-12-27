using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Mdm.References.Dto;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Mdm.Services;

public sealed partial class MdmReferencesQueryService
{
    public async Task<MdmListResultDto<MdmSupplierReferenceDto>> GetSuppliersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query =
            from c in _dbContext.Counterparties.AsNoTracking()
            join r in _dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Supplier && r.IsActive
            select c;

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
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
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FullName ?? string.Empty)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmSupplierReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new MdmListResultDto<MdmSupplierReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmSupplierReferenceDto?> GetSupplierByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query =
            from c in _dbContext.Counterparties.AsNoTracking()
            join r in _dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Supplier && r.IsActive && c.Id == id
            select c;

        return await query
            .Select(x => new MdmSupplierReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MdmListResultDto<MdmCustomerReferenceDto>> GetCustomersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query =
            from c in _dbContext.Counterparties.AsNoTracking()
            join r in _dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Customer && r.IsActive
            select c;

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
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
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FullName ?? string.Empty)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmCustomerReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new MdmListResultDto<MdmCustomerReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmCustomerReferenceDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var query =
            from c in _dbContext.Counterparties.AsNoTracking()
            join r in _dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
            where r.RoleType == CounterpartyRoleTypes.Customer && r.IsActive && c.Id == id
            select c;

        return await query
            .Select(x => new MdmCustomerReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Inn = x.Inn,
                Kpp = x.Kpp,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MdmListResultDto<MdmCounterpartyReferenceDto>> GetCounterpartiesAsync(
        string? q,
        bool? isActive,
        string? roleType,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var parsedRoleType = TryParseRoleType(roleType);

        IQueryable<Counterparty> query = _dbContext.Counterparties.AsNoTracking();

        if (parsedRoleType.HasValue)
        {
            query =
                from c in query
                join r in _dbContext.CounterpartyRoles.AsNoTracking() on c.Id equals r.CounterpartyId
                where r.RoleType == parsedRoleType.Value && r.IsActive
                select c;
        }

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
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
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FullName ?? string.Empty)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmCounterpartyReferenceDto
            {
                Id = x.Id,
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

        if (items.Count == 0)
        {
            return new MdmListResultDto<MdmCounterpartyReferenceDto> { Total = total, Items = items };
        }

        var ids = items.Select(x => x.Id).ToList();

        var roles = await _dbContext.CounterpartyRoles
            .AsNoTracking()
            .Where(r => ids.Contains(r.CounterpartyId))
            .Select(r => new { r.CounterpartyId, r.RoleType, r.IsActive, r.UpdatedAt })
            .ToListAsync(cancellationToken);

        var links = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Counterparty) && ids.Contains(l.EntityId))
            .Select(l => new { l.EntityId, l.ExternalSystem, l.ExternalEntity, l.ExternalId, l.SourceType, l.SyncedAt, l.UpdatedAt })
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
            .GroupBy(x => x.EntityId)
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

        return new MdmListResultDto<MdmCounterpartyReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmCounterpartyReferenceDto?> GetCounterpartyByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Counterparties
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmCounterpartyReferenceDto
            {
                Id = x.Id,
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
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return null;
        }

        entity.Roles = await _dbContext.CounterpartyRoles
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

        entity.ExternalLinks = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Counterparty) && l.EntityId == id)
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

        return entity;
    }
}
