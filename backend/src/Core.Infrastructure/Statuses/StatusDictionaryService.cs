using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Statuses;
using MyIS.Core.Application.Statuses.Dto;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Statuses.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Statuses;

public sealed class StatusDictionaryService : IStatusDictionaryService
{
    private const int RequestFinalFlag = 1;
    private const string SystemExternalSystem = "MyIS";
    private const string RequestsGroupExternalEntity = "RequestStatusGroup";

    private readonly AppDbContext _dbContext;

    public StatusDictionaryService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<StatusListResultDto<StatusGroupDto>> GetGroupsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Statuses.AsNoTracking()
            .Where(x => x.GroupId == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var requestGroupIds = _dbContext.ExternalEntityLinks.AsNoTracking()
            .Where(x =>
                x.EntityType == nameof(Status) &&
                x.ExternalSystem == SystemExternalSystem &&
                x.ExternalEntity == RequestsGroupExternalEntity)
            .Select(x => x.EntityId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.Name)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new StatusGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsRequestsGroup = requestGroupIds.Contains(x.Id),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new StatusListResultDto<StatusGroupDto> { Total = total, Items = items };
    }

    public async Task<StatusGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Statuses.AsNoTracking()
            .Where(x => x.GroupId == null)
            .Where(x => x.Id == id)
            .Select(x => new StatusGroupDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsRequestsGroup = _dbContext.ExternalEntityLinks.AsNoTracking().Any(link =>
                    link.EntityType == nameof(Status) &&
                    link.EntityId == x.Id &&
                    link.ExternalSystem == SystemExternalSystem &&
                    link.ExternalEntity == RequestsGroupExternalEntity),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StatusGroupDto> CreateGroupAsync(
        string name,
        string? description,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var group = new Status(null, name, description, color: null, flags: null, sortOrder: sortOrder);
        if (!isActive)
        {
            group.Deactivate();
        }

        _dbContext.Statuses.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StatusGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            SortOrder = group.SortOrder,
            IsActive = group.IsActive,
            IsRequestsGroup = false,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public async Task<StatusGroupDto> UpdateGroupAsync(
        Guid id,
        string name,
        string? description,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Statuses
            .FirstOrDefaultAsync(x => x.Id == id && x.GroupId == null, cancellationToken);
        if (group == null)
        {
            throw new InvalidOperationException($"StatusGroup '{id}' was not found.");
        }

        group.UpdateFromExternal(name, description, color: group.Color, flags: group.Flags, sortOrder: sortOrder, isActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StatusGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            SortOrder = group.SortOrder,
            IsActive = group.IsActive,
            IsRequestsGroup = await IsRequestsGroupAsync(group.Id, cancellationToken),
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }

    public async Task ArchiveGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await _dbContext.Statuses
            .FirstOrDefaultAsync(x => x.Id == id && x.GroupId == null, cancellationToken);
        if (group == null)
        {
            throw new InvalidOperationException($"StatusGroup '{id}' was not found.");
        }

        group.UpdateFromExternal(group.Name, group.Description, group.Color, group.Flags, group.SortOrder, false);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StatusListResultDto<StatusDto>> GetStatusesAsync(
        string? q,
        Guid? groupId,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query =
            from status in _dbContext.Statuses.AsNoTracking()
            where status.GroupId != null
            join statusGroup in _dbContext.Statuses.AsNoTracking()
                on status.GroupId equals statusGroup.Id
            select new { status, statusGroup };

        if (groupId.HasValue)
        {
            query = query.Where(x => x.status.GroupId == groupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x =>
                x.status.Name.Contains(search) ||
                x.statusGroup.Name.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.status.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.statusGroup.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.statusGroup.Name)
            .ThenBy(x => x.status.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.status.Name)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new StatusDto
            {
                Id = x.status.Id,
                GroupId = x.statusGroup.Id,
                GroupName = x.statusGroup.Name,
                Name = x.status.Name,
                Color = x.status.Color,
                Flags = x.status.Flags,
                SortOrder = x.status.SortOrder,
                IsActive = x.status.IsActive,
                CreatedAt = x.status.CreatedAt,
                UpdatedAt = x.status.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new StatusListResultDto<StatusDto> { Total = total, Items = items };
    }

    public async Task<StatusDto?> GetStatusByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (
            from status in _dbContext.Statuses.AsNoTracking()
            join statusGroup in _dbContext.Statuses.AsNoTracking()
                on status.GroupId equals statusGroup.Id
            where status.Id == id
            select new StatusDto
            {
                Id = status.Id,
                GroupId = statusGroup.Id,
                GroupName = statusGroup.Name,
                Name = status.Name,
                Color = status.Color,
                Flags = status.Flags,
                SortOrder = status.SortOrder,
                IsActive = status.IsActive,
                CreatedAt = status.CreatedAt,
                UpdatedAt = status.UpdatedAt
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StatusDto> CreateStatusAsync(
        Guid groupId,
        string name,
        int? color,
        int? flags,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Statuses
            .FirstOrDefaultAsync(x => x.Id == groupId && x.GroupId == null, cancellationToken);
        if (group == null)
        {
            throw new InvalidOperationException($"StatusGroup '{groupId}' was not found.");
        }

        var status = new Status(group.Id, name, description: null, color, flags, sortOrder);
        if (!isActive)
        {
            status.Deactivate();
        }

        _dbContext.Statuses.Add(status);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SyncRequestStatusAsync(status, group.Id, cancellationToken);

        return await GetStatusByIdAsync(status.Id, cancellationToken)
               ?? throw new InvalidOperationException("Failed to load created status.");
    }

    public async Task<StatusDto> UpdateStatusAsync(
        Guid id,
        Guid groupId,
        string name,
        int? color,
        int? flags,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var status = await _dbContext.Statuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (status == null)
        {
            throw new InvalidOperationException($"Status '{id}' was not found.");
        }

        var oldGroup = status.GroupId.HasValue
            ? await _dbContext.Statuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == status.GroupId && x.GroupId == null, cancellationToken)
            : null;

        var newGroup = await _dbContext.Statuses
            .FirstOrDefaultAsync(x => x.Id == groupId && x.GroupId == null, cancellationToken);
        if (newGroup == null)
        {
            throw new InvalidOperationException($"StatusGroup '{groupId}' was not found.");
        }

        status.ChangeGroup(groupId);
        status.UpdateFromExternal(name, description: null, color, flags, sortOrder, isActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (oldGroup != null && await IsRequestsGroupAsync(oldGroup.Id, cancellationToken))
        {
            await SyncRequestStatusArchiveAsync(status.Id, cancellationToken);
        }

        await SyncRequestStatusAsync(status, newGroup.Id, cancellationToken);

        return await GetStatusByIdAsync(status.Id, cancellationToken)
               ?? throw new InvalidOperationException("Failed to load updated status.");
    }

    public async Task ArchiveStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var status = await _dbContext.Statuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (status == null)
        {
            throw new InvalidOperationException($"Status '{id}' was not found.");
        }

        status.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        var group = status.GroupId.HasValue
            ? await _dbContext.Statuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == status.GroupId && x.GroupId == null, cancellationToken)
            : null;

        if (group != null && await IsRequestsGroupAsync(group.Id, cancellationToken))
        {
            await SyncRequestStatusArchiveAsync(status.Id, cancellationToken);
        }
    }

    private async Task SyncRequestStatusAsync(Status status, Guid groupId, CancellationToken cancellationToken)
    {
        if (!await IsRequestsGroupAsync(groupId, cancellationToken))
        {
            return;
        }

        var isFinal = ((status.Flags ?? 0) & RequestFinalFlag) == RequestFinalFlag;
        var statusId = status.Id;

        var existing = await ResolveRequestStatusAsync(statusId, status.Name, cancellationToken);

        if (existing == null)
        {
            var statusCode = new RequestStatusCode(statusId.ToString());
            var created = new RequestStatus(
                RequestStatusId.New(),
                statusCode,
                status.Name,
                isFinal,
                description: null,
                isActive: status.IsActive);
            _dbContext.RequestStatuses.Add(created);
            await EnsureRequestStatusLinkAsync(statusId, created.Id.Value, cancellationToken);
        }
        else
        {
            existing.Rename(status.Name);
            if (isFinal)
            {
                existing.MarkFinal();
            }
            else
            {
                existing.MarkNonFinal();
            }

            if (status.IsActive)
            {
                existing.Activate();
            }
            else
            {
                existing.Deactivate();
            }

            await EnsureRequestStatusLinkAsync(statusId, existing.Id.Value, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncRequestStatusArchiveAsync(Guid statusId, CancellationToken cancellationToken)
    {
        var existing = await ResolveRequestStatusAsync(statusId, null, cancellationToken);

        if (existing == null)
        {
            return;
        }

        existing.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsRequestsGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .AnyAsync(x =>
                x.EntityType == nameof(Status) &&
                x.EntityId == groupId &&
                x.ExternalSystem == SystemExternalSystem &&
                x.ExternalEntity == RequestsGroupExternalEntity,
                cancellationToken);
    }

    private async Task<RequestStatus?> ResolveRequestStatusAsync(Guid statusId, string? statusName, CancellationToken cancellationToken)
    {
        var link = await _dbContext.ExternalEntityLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EntityType == nameof(Status) &&
                x.EntityId == statusId &&
                x.ExternalSystem == "MyIS" &&
                x.ExternalEntity == "RequestStatus",
                cancellationToken);

        if (link != null && Guid.TryParse(link.ExternalId, out var requestStatusId))
        {
            var linked = await _dbContext.RequestStatuses
                .FirstOrDefaultAsync(x => x.Id == new RequestStatusId(requestStatusId), cancellationToken);
            if (linked != null)
            {
                return linked;
            }
        }

        if (!string.IsNullOrWhiteSpace(statusName))
        {
            var byName = await _dbContext.RequestStatuses
                .FirstOrDefaultAsync(x => x.Name == statusName.Trim(), cancellationToken);
            if (byName != null)
            {
                return byName;
            }
        }

        return null;
    }

    private async Task EnsureRequestStatusLinkAsync(Guid statusId, Guid requestStatusId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ExternalEntityLinks
            .FirstOrDefaultAsync(x =>
                x.EntityType == nameof(Status) &&
                x.EntityId == statusId &&
                x.ExternalSystem == "MyIS" &&
                x.ExternalEntity == "RequestStatus",
                cancellationToken);

        var externalId = requestStatusId.ToString();
        if (existing != null)
        {
            _dbContext.Entry(existing).Property(x => x.ExternalId).CurrentValue = externalId;
            _dbContext.Entry(existing).Property(x => x.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var existingByExternalId = await _dbContext.ExternalEntityLinks
            .FirstOrDefaultAsync(x =>
                x.EntityType == nameof(Status) &&
                x.ExternalSystem == "MyIS" &&
                x.ExternalEntity == "RequestStatus" &&
                x.ExternalId == externalId,
                cancellationToken);

        if (existingByExternalId != null)
        {
            existingByExternalId.UpdateEntityId(statusId, now);
            return;
        }

        _dbContext.ExternalEntityLinks.Add(new ExternalEntityLink(
            nameof(Status),
            statusId,
            "MyIS",
            "RequestStatus",
            externalId,
            sourceType: null,
            syncedAt: now));
    }
}
