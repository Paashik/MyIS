using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Security.Commands.Admin;
using MyIS.Core.Application.Security.Dto;
using MyIS.Core.Application.Security.Handlers.Admin;
using MyIS.Core.Application.Security.Queries.Admin;
using MyIS.Core.WebApi.Contracts.Admin.Security;

namespace MyIS.Core.WebApi.Controllers.Admin.Security;

[ApiController]
[Route("api/admin/security/roles")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
[Authorize(Policy = "Admin.Security.View")]
public sealed class AdminRolesController : ControllerBase
{
    private readonly GetAdminRolesHandler _getHandler;
    private readonly CreateAdminRoleHandler _createHandler;
    private readonly UpdateAdminRoleHandler _updateHandler;
    private readonly ILogger<AdminRolesController> _logger;

    public AdminRolesController(
        GetAdminRolesHandler getHandler,
        CreateAdminRoleHandler createHandler,
        UpdateAdminRoleHandler updateHandler,
        ILogger<AdminRolesController> logger)
    {
        _getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminRolesQuery { CurrentUserId = currentUserId };
        var result = await _getHandler.Handle(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Admin.Security.EditRoles")]
    public async Task<ActionResult<RoleDto>> Create([FromBody] AdminRoleCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new CreateAdminRoleCommand { CurrentUserId = currentUserId, Code = request.Code, Name = request.Name };
        try
        {
            var dto = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/admin/security/roles/{dto.Id}", dto);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin.Security.EditRoles")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] AdminRoleUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new UpdateAdminRoleCommand { CurrentUserId = currentUserId, Id = id, Name = request.Name };
        try
        {
            var dto = await _updateHandler.Handle(command, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
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

