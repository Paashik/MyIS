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
    public async Task<MdmListResultDto<MdmItemReferenceDto>> GetItemsAsync(
        string? q,
        bool? isActive,
        Guid? groupId,
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
            var qLike = $"%{q}%";
            query = query.Where(x =>
                (x.Code != null && EF.Functions.ILike(x.Code, qLike)) ||
                EF.Functions.ILike(x.NomenclatureNo, qLike) ||
                EF.Functions.ILike(x.Name, qLike) ||
                (x.Designation != null && EF.Functions.ILike(x.Designation, qLike)) ||
                (x.ManufacturerPartNumber != null && EF.Functions.ILike(x.ManufacturerPartNumber, qLike)) ||
                _dbContext.ExternalEntityLinks.Any(l =>
                    l.EntityType == nameof(Item)
                    && l.EntityId == x.Id
                    && EF.Functions.ILike(l.ExternalId, qLike)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (groupId.HasValue)
        {
            query = query.Where(x => x.ItemGroupId == groupId.Value);
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

        var latestLink = ExternalEntityLinkSelector.SelectLatestLink(links);
        if (latestLink != null)
        {
            dto.ExternalSystem = latestLink.ExternalSystem;
            dto.ExternalId = latestLink.ExternalId;
            dto.SyncedAt = latestLink.SyncedAt;
        }

        return dto;
    }
}
