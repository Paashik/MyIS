using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Statuses.Dto;

namespace MyIS.Core.Application.Statuses;

public interface IStatusDictionaryService
{
    Task<StatusListResultDto<StatusGroupDto>> GetGroupsAsync(
        string? q,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<StatusGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StatusGroupDto> CreateGroupAsync(
        string name,
        string? description,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task<StatusGroupDto> UpdateGroupAsync(
        Guid id,
        string name,
        string? description,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task ArchiveGroupAsync(Guid id, CancellationToken cancellationToken);

    Task<StatusListResultDto<StatusDto>> GetStatusesAsync(
        string? q,
        Guid? groupId,
        bool? isActive,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<StatusDto?> GetStatusByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StatusDto> CreateStatusAsync(
        Guid groupId,
        string name,
        int? color,
        int? flags,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task<StatusDto> UpdateStatusAsync(
        Guid id,
        Guid groupId,
        string name,
        int? color,
        int? flags,
        int? sortOrder,
        bool isActive,
        CancellationToken cancellationToken);

    Task ArchiveStatusAsync(Guid id, CancellationToken cancellationToken);
}
