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
[Route("api/admin/security/users")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
[Authorize(Policy = "Admin.Security.View")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly GetAdminUsersHandler _getHandler;
    private readonly GetAdminUserByIdHandler _getByIdHandler;
    private readonly CreateAdminUserHandler _createHandler;
    private readonly UpdateAdminUserHandler _updateHandler;
    private readonly ActivateAdminUserHandler _activateHandler;
    private readonly DeactivateAdminUserHandler _deactivateHandler;
    private readonly ResetAdminUserPasswordHandler _resetPasswordHandler;
    private readonly GetAdminUserRolesHandler _getRolesHandler;
    private readonly ReplaceAdminUserRolesHandler _replaceRolesHandler;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        GetAdminUsersHandler getHandler,
        GetAdminUserByIdHandler getByIdHandler,
        CreateAdminUserHandler createHandler,
        UpdateAdminUserHandler updateHandler,
        ActivateAdminUserHandler activateHandler,
        DeactivateAdminUserHandler deactivateHandler,
        ResetAdminUserPasswordHandler resetPasswordHandler,
        GetAdminUserRolesHandler getRolesHandler,
        ReplaceAdminUserRolesHandler replaceRolesHandler,
        ILogger<AdminUsersController> logger)
    {
        _getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        _getByIdHandler = getByIdHandler ?? throw new ArgumentNullException(nameof(getByIdHandler));
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _activateHandler = activateHandler ?? throw new ArgumentNullException(nameof(activateHandler));
        _deactivateHandler = deactivateHandler ?? throw new ArgumentNullException(nameof(deactivateHandler));
        _resetPasswordHandler = resetPasswordHandler ?? throw new ArgumentNullException(nameof(resetPasswordHandler));
        _getRolesHandler = getRolesHandler ?? throw new ArgumentNullException(nameof(getRolesHandler));
        _replaceRolesHandler = replaceRolesHandler ?? throw new ArgumentNullException(nameof(replaceRolesHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminUsersQuery { CurrentUserId = currentUserId, Search = search, IsActive = isActive };
        var result = await _getHandler.Handle(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminUserByIdQuery { CurrentUserId = currentUserId, Id = id };
        try
        {
            var dto = await _getByIdHandler.Handle(query, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "Admin.Security.EditUsers")]
    public async Task<ActionResult<UserDetailsDto>> Create([FromBody] AdminUserCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new CreateAdminUserCommand
        {
            CurrentUserId = currentUserId,
            Login = request.Login,
            Password = request.Password,
            IsActive = request.IsActive,
            EmployeeId = request.EmployeeId
        };

        try
        {
            var dto = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/admin/security/users/{dto.Id}", dto);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin.Security.EditUsers")]
    public async Task<ActionResult<UserDetailsDto>> Update(Guid id, [FromBody] AdminUserUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new UpdateAdminUserCommand
        {
            CurrentUserId = currentUserId,
            Id = id,
            Login = request.Login,
            IsActive = request.IsActive,
            EmployeeId = request.EmployeeId
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
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "Admin.Security.EditUsers")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ActivateAdminUserCommand { CurrentUserId = currentUserId, Id = id };
        try
        {
            await _activateHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = "Admin.Security.EditUsers")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new DeactivateAdminUserCommand { CurrentUserId = currentUserId, Id = id };
        try
        {
            await _deactivateHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = "Admin.Security.EditUsers")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminUserResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ResetAdminUserPasswordCommand { CurrentUserId = currentUserId, Id = id, NewPassword = request.NewPassword };
        try
        {
            await _resetPasswordHandler.Handle(command, cancellationToken);
            return NoContent();
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

    [HttpGet("{id:guid}/roles")]
    [Authorize(Policy = "Admin.Security.EditRoles")]
    public async Task<ActionResult<UserRolesDto>> GetRoles(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminUserRolesQuery { CurrentUserId = currentUserId, UserId = id };
        try
        {
            var dto = await _getRolesHandler.Handle(query, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = "Admin.Security.EditRoles")]
    public async Task<IActionResult> ReplaceRoles(Guid id, [FromBody] AdminReplaceUserRolesRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ReplaceAdminUserRolesCommand { CurrentUserId = currentUserId, UserId = id, RoleIds = request.RoleIds };
        try
        {
            await _replaceRolesHandler.Handle(command, cancellationToken);
            return NoContent();
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

