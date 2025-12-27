using System;
using System.Collections.Generic;
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

        var latestLink = ExternalEntityLinkSelector.SelectLatestLink(links);
        if (latestLink != null)
        {
            unit.ExternalSystem = latestLink.ExternalSystem;
            unit.ExternalId = latestLink.ExternalId;
            unit.SyncedAt = latestLink.SyncedAt;
        }

        return unit;
    }
}
