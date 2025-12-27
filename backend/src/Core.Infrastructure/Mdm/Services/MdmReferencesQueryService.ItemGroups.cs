using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Mdm.References.Dto;

namespace MyIS.Core.Infrastructure.Mdm.Services;

public sealed partial class MdmReferencesQueryService
{
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
}
