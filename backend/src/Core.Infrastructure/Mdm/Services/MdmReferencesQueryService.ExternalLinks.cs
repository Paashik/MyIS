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
