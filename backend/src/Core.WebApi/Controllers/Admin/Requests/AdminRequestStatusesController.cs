using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Requests.Commands.Admin;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers.Admin;
using MyIS.Core.Application.Requests.Queries.Admin;
using MyIS.Core.WebApi.Contracts.Admin.Requests;

namespace MyIS.Core.WebApi.Controllers.Admin.Requests;

[ApiController]
[Route("api/admin/requests/statuses")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
[Authorize(Policy = "Admin.Requests.EditStatuses")]
public sealed class AdminRequestStatusesController : ControllerBase
{
    private readonly GetAdminRequestStatusesHandler _getHandler;
    private readonly CreateAdminRequestStatusHandler _createHandler;
    private readonly UpdateAdminRequestStatusHandler _updateHandler;
    private readonly ArchiveAdminRequestStatusHandler _archiveHandler;
    private readonly ILogger<AdminRequestStatusesController> _logger;

    public AdminRequestStatusesController(
        GetAdminRequestStatusesHandler getHandler,
        CreateAdminRequestStatusHandler createHandler,
        UpdateAdminRequestStatusHandler updateHandler,
        ArchiveAdminRequestStatusHandler archiveHandler,
        ILogger<AdminRequestStatusesController> logger)
    {
        _getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _archiveHandler = archiveHandler ?? throw new ArgumentNullException(nameof(archiveHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RequestStatusDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminRequestStatusesQuery { CurrentUserId = currentUserId };
        var result = await _getHandler.Handle(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RequestStatusDto>> Create(
        [FromBody] AdminRequestStatusCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new CreateAdminRequestStatusCommand
        {
            CurrentUserId = currentUserId,
            Code = request.Code,
            Name = request.Name,
            IsFinal = request.IsFinal,
            Description = request.Description,
            IsActive = request.IsActive
        };

        try
        {
            var dto = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/admin/requests/statuses/{dto.Id}", dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RequestStatusDto>> Update(
        Guid id,
        [FromBody] AdminRequestStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new UpdateAdminRequestStatusCommand
        {
            CurrentUserId = currentUserId,
            Id = id,
            Name = request.Name,
            IsFinal = request.IsFinal,
            Description = request.Description,
            IsActive = request.IsActive
        };

        try
        {
            var dto = await _updateHandler.Handle(command, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ArchiveAdminRequestStatusCommand
        {
            CurrentUserId = currentUserId,
            Id = id
        };

        try
        {
            await _archiveHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(raw))
        {
            _logger.LogWarning("Current user has no NameIdentifier claim.");
            return false;
        }

        if (!Guid.TryParse(raw, out userId))
        {
            _logger.LogWarning("Failed to parse NameIdentifier claim '{Claim}' as Guid.", raw);
            userId = Guid.Empty;
            return false;
        }

        return true;
    }
}

