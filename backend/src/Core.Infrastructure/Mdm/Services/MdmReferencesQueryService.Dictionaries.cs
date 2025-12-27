using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Mdm.References.Dto;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.Services;

namespace MyIS.Core.Infrastructure.Mdm.Services;

public sealed partial class MdmReferencesQueryService
{
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

            var latestLinks = ExternalEntityLinkSelector.SelectLatestLinks(links);
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

        var latestLink = ExternalEntityLinkSelector.SelectLatestLink(links);
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

            var latestLinks = ExternalEntityLinkSelector.SelectLatestLinks(links);
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

        var latestLink = ExternalEntityLinkSelector.SelectLatestLink(links);
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
}
