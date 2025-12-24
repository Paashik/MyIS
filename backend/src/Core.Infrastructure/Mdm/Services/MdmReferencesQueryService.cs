using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Mdm.References.Dto;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Services;

public sealed class MdmReferencesQueryService : IMdmReferencesQueryService
{
    private readonly AppDbContext _dbContext;

    public MdmReferencesQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

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

    private static ExternalEntityLink? SelectLatestLink(IEnumerable<ExternalEntityLink> links)
    {
        return links
            .OrderByDescending(l => l.SyncedAt ?? l.UpdatedAt)
            .ThenByDescending(l => l.UpdatedAt)
            .FirstOrDefault();
    }

    private static Dictionary<Guid, ExternalEntityLink> SelectLatestLinks(IEnumerable<ExternalEntityLink> links)
    {
        return links
            .GroupBy(l => l.EntityId)
            .ToDictionary(g => g.Key, g => SelectLatestLink(g)!);
    }

    public async Task<MdmListResultDto<MdmUnitReferenceDto>> GetUnitsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.UnitOfMeasures.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                x.Symbol.Contains(q) ||
                _dbContext.ExternalEntityLinks.Any(l =>
                    l.EntityType == nameof(UnitOfMeasure)
                    && l.EntityId == x.Id
                    && l.ExternalId.Contains(q)));
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
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var ids = items.Select(x => x.Id).ToList();
            var links = await _dbContext.ExternalEntityLinks
                .AsNoTracking()
                .Where(l => l.EntityType == nameof(UnitOfMeasure) && ids.Contains(l.EntityId))
                .ToListAsync(cancellationToken);

            var latestLinks = SelectLatestLinks(links);
            foreach (var item in items)
            {
                if (latestLinks.TryGetValue(item.Id, out var link))
                {
                    item.ExternalSystem = link.ExternalSystem;
                    item.ExternalId = link.ExternalId;
                    item.SyncedAt = link.SyncedAt;
                }
            }
        }

        return new MdmListResultDto<MdmUnitReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmUnitReferenceDto?> GetUnitByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var unit = await _dbContext.UnitOfMeasures
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmUnitReferenceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Symbol = x.Symbol,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit == null)
        {
            return null;
        }

        var links = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(UnitOfMeasure) && l.EntityId == id)
            .ToListAsync(cancellationToken);

        var latestLink = SelectLatestLink(links);
        if (latestLink != null)
        {
            unit.ExternalSystem = latestLink.ExternalSystem;
            unit.ExternalId = latestLink.ExternalId;
            unit.SyncedAt = latestLink.SyncedAt;
        }

        return unit;
    }

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

    public async Task<MdmListResultDto<MdmItemGroupReferenceDto>> GetItemGroupsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query =
            from g in _dbContext.ItemGroups.AsNoTracking()
            join p in _dbContext.ItemGroups.AsNoTracking() on g.ParentId equals p.Id into parents
            from p in parents.DefaultIfEmpty()
            select new { Group = g, Parent = p };

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                x.Group.Name.Contains(q) ||
                (x.Group.Abbreviation != null && x.Group.Abbreviation.Contains(q)) ||
                (x.Parent != null && x.Parent.Name.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.Group.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Group.Name)
            .Skip(skip)
            .Take(take)
            .Select(x => new MdmItemGroupReferenceDto
            {
                Id = x.Group.Id,
                Name = x.Group.Name,
                Abbreviation = x.Group.Abbreviation,
                ParentId = x.Group.ParentId,
                ParentName = x.Parent != null ? x.Parent.Name : null,
                IsActive = x.Group.IsActive,
                UpdatedAt = x.Group.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new MdmListResultDto<MdmItemGroupReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmItemGroupReferenceDto?> GetItemGroupByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
                from g in _dbContext.ItemGroups.AsNoTracking()
                where g.Id == id
                join p in _dbContext.ItemGroups.AsNoTracking() on g.ParentId equals p.Id into parents
                from p in parents.DefaultIfEmpty()
                select new MdmItemGroupReferenceDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Abbreviation = g.Abbreviation,
                    ParentId = g.ParentId,
                    ParentName = p != null ? p.Name : null,
                    IsActive = g.IsActive,
                    UpdatedAt = g.UpdatedAt
                })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MdmListResultDto<MdmItemReferenceDto>> GetItemsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Items
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.ItemGroup)
            .AsQueryable();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.NomenclatureNo.Contains(q) ||
                x.Name.Contains(q) ||
                (x.Designation != null && x.Designation.Contains(q)) ||
                (x.ManufacturerPartNumber != null && x.ManufacturerPartNumber.Contains(q)) ||
                _dbContext.ExternalEntityLinks.Any(l =>
                    l.EntityType == nameof(Item)
                    && l.EntityId == x.Id
                    && l.ExternalId.Contains(q)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);

        var groups = await _dbContext.ItemGroups
            .AsNoTracking()
            .Select(g => new
            {
                g.Id,
                g.ParentId,
                g.Name
            })
            .ToListAsync(cancellationToken);

        var groupsById = groups.ToDictionary(x => x.Id);

        var page = await query
            .OrderBy(x => x.NomenclatureNo)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = page.Select(item =>
        {
            var group = item.ItemGroupId.HasValue && groupsById.TryGetValue(item.ItemGroupId.Value, out var g) ? g : null;
            var category = group;
            while (category?.ParentId != null && groupsById.TryGetValue(category.ParentId.Value, out var parent))
            {
                category = parent;
            }

            return new MdmItemReferenceDto
            {
                Id = item.Id,
                Code = item.Code,
                NomenclatureNo = item.NomenclatureNo,
                Name = item.Name,
                Designation = item.Designation,
                ItemKind = category?.Name ?? item.ItemKind.ToString(),
                IsEskd = item.IsEskd,
                IsEskdDocument = item.IsEskdDocument,
                ManufacturerPartNumber = item.ManufacturerPartNumber,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                UnitOfMeasureId = item.UnitOfMeasureId,
                UnitOfMeasureCode = item.UnitOfMeasure?.Code,
                UnitOfMeasureName = item.UnitOfMeasure?.Name,
                UnitOfMeasureSymbol = item.UnitOfMeasure?.Symbol,
                ItemGroupId = item.ItemGroupId,
                        ItemGroupName = group?.Name ?? item.ItemGroup?.Name,
                        CategoryId = category?.Id,
                        CategoryName = category?.Name
                    };
                }).ToList();

        if (items.Count > 0)
        {
            var ids = items.Select(x => x.Id).ToList();
            var links = await _dbContext.ExternalEntityLinks
                .AsNoTracking()
                .Where(l => l.EntityType == nameof(Item) && ids.Contains(l.EntityId))
                .ToListAsync(cancellationToken);

            var latestLinks = SelectLatestLinks(links);
            foreach (var item in items)
            {
                if (latestLinks.TryGetValue(item.Id, out var link))
                {
                    item.ExternalSystem = link.ExternalSystem;
                    item.ExternalId = link.ExternalId;
                    item.SyncedAt = link.SyncedAt;
                }
            }
        }

        return new MdmListResultDto<MdmItemReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmItemReferenceDto?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _dbContext.Items
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.ItemGroup)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return null;
        }

        var category = item.ItemGroup;
        while (category?.ParentId != null)
        {
            category = await _dbContext.ItemGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == category.ParentId, cancellationToken);
        }

        var dto = new MdmItemReferenceDto
        {
            Id = item.Id,
            Code = item.Code,
            NomenclatureNo = item.NomenclatureNo,
            Name = item.Name,
            Designation = item.Designation,
            ItemKind = category?.Name ?? item.ItemKind.ToString(),
            IsEskd = item.IsEskd,
            IsEskdDocument = item.IsEskdDocument,
            ManufacturerPartNumber = item.ManufacturerPartNumber,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            UnitOfMeasureId = item.UnitOfMeasureId,
            UnitOfMeasureCode = item.UnitOfMeasure?.Code,
            UnitOfMeasureName = item.UnitOfMeasure?.Name,
            UnitOfMeasureSymbol = item.UnitOfMeasure?.Symbol,
            ItemGroupId = item.ItemGroupId,
            ItemGroupName = item.ItemGroup?.Name,
            CategoryId = category?.Id,
            CategoryName = category?.Name
        };

        var links = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Item) && l.EntityId == id)
            .ToListAsync(cancellationToken);

        var latestLink = SelectLatestLink(links);
        if (latestLink != null)
        {
            dto.ExternalSystem = latestLink.ExternalSystem;
            dto.ExternalId = latestLink.ExternalId;
            dto.SyncedAt = latestLink.SyncedAt;
        }

        return dto;
    }

    public async Task<MdmListResultDto<MdmManufacturerReferenceDto>> GetManufacturersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Manufacturers.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                x.Name.Contains(q) ||
                (x.FullName != null && x.FullName.Contains(q)) ||
                (x.Site != null && x.Site.Contains(q)) ||
                _dbContext.ExternalEntityLinks.Any(l =>
                    l.EntityType == nameof(Manufacturer)
                    && l.EntityId == x.Id
                    && l.ExternalId.Contains(q)));
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
            .Select(x => new MdmManufacturerReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Site = x.Site,
                Note = x.Note,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var ids = items.Select(x => x.Id).ToList();
            var links = await _dbContext.ExternalEntityLinks
                .AsNoTracking()
                .Where(l => l.EntityType == nameof(Manufacturer) && ids.Contains(l.EntityId))
                .ToListAsync(cancellationToken);

            var latestLinks = SelectLatestLinks(links);
            foreach (var item in items)
            {
                if (latestLinks.TryGetValue(item.Id, out var link))
                {
                    item.ExternalSystem = link.ExternalSystem;
                    item.ExternalId = link.ExternalId;
                    item.SyncedAt = link.SyncedAt;
                }
            }
        }

        return new MdmListResultDto<MdmManufacturerReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmManufacturerReferenceDto?> GetManufacturerByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var manufacturer = await _dbContext.Manufacturers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MdmManufacturerReferenceDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName,
                Site = x.Site,
                Note = x.Note,
                IsActive = x.IsActive,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (manufacturer == null)
        {
            return null;
        }

        var links = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Manufacturer) && l.EntityId == id)
            .ToListAsync(cancellationToken);

        var latestLink = SelectLatestLink(links);
        if (latestLink != null)
        {
            manufacturer.ExternalSystem = latestLink.ExternalSystem;
            manufacturer.ExternalId = latestLink.ExternalId;
            manufacturer.SyncedAt = latestLink.SyncedAt;
        }

        return manufacturer;
    }

    public async Task<MdmListResultDto<MdmSimpleReferenceDto>> GetBodyTypesAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.BodyTypes.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x => x.Name.Contains(q));
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

        return new MdmListResultDto<MdmSimpleReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmSimpleReferenceDto?> GetBodyTypeByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.BodyTypes
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
    }

    public async Task<MdmListResultDto<MdmCurrencyReferenceDto>> GetCurrenciesAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Currencies.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            query = query.Where(x =>
                (x.Code != null && x.Code.Contains(q)) ||
                x.Name.Contains(q) ||
                (x.Symbol != null && x.Symbol.Contains(q)) ||
                _dbContext.ExternalEntityLinks.Any(l =>
                    l.EntityType == nameof(Currency)
                    && l.EntityId == x.Id
                    && l.ExternalId.Contains(q)));
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
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var ids = items.Select(x => x.Id).ToList();
            var links = await _dbContext.ExternalEntityLinks
                .AsNoTracking()
                .Where(l => l.EntityType == nameof(Currency) && ids.Contains(l.EntityId))
                .ToListAsync(cancellationToken);

            var latestLinks = SelectLatestLinks(links);
            foreach (var item in items)
            {
                if (latestLinks.TryGetValue(item.Id, out var link))
                {
                    item.ExternalSystem = link.ExternalSystem;
                    item.ExternalId = link.ExternalId;
                    item.SyncedAt = link.SyncedAt;
                }
            }
        }

        return new MdmListResultDto<MdmCurrencyReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmCurrencyReferenceDto?> GetCurrencyByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var currency = await _dbContext.Currencies
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
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (currency == null)
        {
            return null;
        }

        var links = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .Where(l => l.EntityType == nameof(Currency) && l.EntityId == id)
            .ToListAsync(cancellationToken);

        var latestLink = SelectLatestLink(links);
        if (latestLink != null)
        {
            currency.ExternalSystem = latestLink.ExternalSystem;
            currency.ExternalId = latestLink.ExternalId;
            currency.SyncedAt = latestLink.SyncedAt;
        }

        return currency;
    }

    public async Task<MdmListResultDto<MdmSimpleReferenceDto>> GetTechnicalParametersAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TechnicalParameters.AsNoTracking();

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

        return new MdmListResultDto<MdmSimpleReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmSimpleReferenceDto?> GetTechnicalParameterByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.TechnicalParameters
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
    }

    public async Task<MdmListResultDto<MdmSimpleReferenceDto>> GetParameterSetsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ParameterSets.AsNoTracking();

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

        return new MdmListResultDto<MdmSimpleReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmSimpleReferenceDto?> GetParameterSetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.ParameterSets
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
    }

    public async Task<MdmListResultDto<MdmSimpleReferenceDto>> GetSymbolsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Symbols.AsNoTracking();

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

        return new MdmListResultDto<MdmSimpleReferenceDto> { Total = total, Items = items };
    }

    public async Task<MdmSimpleReferenceDto?> GetSymbolByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Symbols
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
    }

    public async Task<MdmListResultDto<MdmExternalEntityLinkDto>> GetExternalEntityLinksAsync(
        string? q,
        string? entityType,
        string? externalSystem,
        string? externalEntity,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ExternalEntityLinks.AsNoTracking();

        q = NormalizeQuery(q);
        if (q != null)
        {
            Guid? entityIdFilter = null;
            if (Guid.TryParse(q, out var parsedId))
            {
                entityIdFilter = parsedId;
            }

            query = query.Where(l =>
                l.EntityType.Contains(q) ||
                l.ExternalSystem.Contains(q) ||
                l.ExternalEntity.Contains(q) ||
                l.ExternalId.Contains(q) ||
                (entityIdFilter.HasValue && l.EntityId == entityIdFilter.Value));
        }

        entityType = NormalizeQuery(entityType);
        if (entityType != null)
        {
            query = query.Where(l => l.EntityType == entityType);
        }

        externalSystem = NormalizeQuery(externalSystem);
        if (externalSystem != null)
        {
            query = query.Where(l => l.ExternalSystem == externalSystem);
        }

        externalEntity = NormalizeQuery(externalEntity);
        if (externalEntity != null)
        {
            query = query.Where(l => l.ExternalEntity == externalEntity);
        }

        skip = Math.Max(0, skip);
        take = Clamp(take, 1, 1000);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(l => l.EntityType)
            .ThenBy(l => l.ExternalSystem)
            .ThenBy(l => l.ExternalEntity)
            .ThenBy(l => l.ExternalId)
            .Skip(skip)
            .Take(take)
            .Select(l => new MdmExternalEntityLinkDto
            {
                Id = l.Id,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                ExternalSystem = l.ExternalSystem,
                ExternalEntity = l.ExternalEntity,
                ExternalId = l.ExternalId,
                SourceType = l.SourceType,
                SyncedAt = l.SyncedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new MdmListResultDto<MdmExternalEntityLinkDto> { Total = total, Items = items };
    }
}
