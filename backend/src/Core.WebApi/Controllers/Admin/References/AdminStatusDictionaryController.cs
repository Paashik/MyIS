using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyIS.Core.Application.Statuses;

namespace MyIS.Core.WebApi.Controllers.Admin.References;

[ApiController]
[Route("api/admin/references/mdm")]
public sealed class AdminStatusDictionaryController : ControllerBase
{
    private readonly IStatusDictionaryService _service;

    public AdminStatusDictionaryController(IStatusDictionaryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("status-groups")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetGroups(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetGroupsAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("status-groups/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetGroupByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpPost("status-groups")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> CreateGroup([FromBody] StatusGroupUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateGroupAsync(
            request.Name,
            request.Description,
            request.SortOrder,
            request.IsActive ?? true,
            cancellationToken);
        return Ok(created);
    }

    [HttpPut("status-groups/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> UpdateGroup(
        Guid id,
        [FromBody] StatusGroupUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateGroupAsync(
            id,
            request.Name,
            request.Description,
            request.SortOrder,
            request.IsActive ?? true,
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("status-groups/{id:guid}/archive")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> ArchiveGroup(Guid id, CancellationToken cancellationToken)
    {
        await _service.ArchiveGroupAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("statuses")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetStatuses(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] Guid? groupId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetStatusesAsync(q, groupId, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("statuses/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetStatusById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetStatusByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpPost("statuses")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> CreateStatus([FromBody] StatusUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateStatusAsync(
            request.GroupId,
            request.Name,
            request.Color,
            request.Flags,
            request.SortOrder,
            request.IsActive ?? true,
            cancellationToken);
        return Ok(created);
    }

    [HttpPut("statuses/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] StatusUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateStatusAsync(
            id,
            request.GroupId,
            request.Name,
            request.Color,
            request.Flags,
            request.SortOrder,
            request.IsActive ?? true,
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("statuses/{id:guid}/archive")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> ArchiveStatus(Guid id, CancellationToken cancellationToken)
    {
        await _service.ArchiveStatusAsync(id, cancellationToken);
        return NoContent();
    }

    public sealed class StatusGroupUpsertRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int? SortOrder { get; init; }
        public bool? IsActive { get; init; }
    }

    public sealed class StatusUpsertRequest
    {
        public Guid GroupId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? Color { get; init; }
        public int? Flags { get; init; }
        public int? SortOrder { get; init; }
        public bool? IsActive { get; init; }
    }
}
