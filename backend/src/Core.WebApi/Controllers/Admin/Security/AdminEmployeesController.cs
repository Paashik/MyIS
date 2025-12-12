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
[Route("api/admin/security/employees")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
[Authorize(Policy = "Admin.Security.View")]
public sealed class AdminEmployeesController : ControllerBase
{
    private readonly GetAdminEmployeesHandler _getHandler;
    private readonly GetAdminEmployeeByIdHandler _getByIdHandler;
    private readonly CreateAdminEmployeeHandler _createHandler;
    private readonly UpdateAdminEmployeeHandler _updateHandler;
    private readonly ActivateAdminEmployeeHandler _activateHandler;
    private readonly DeactivateAdminEmployeeHandler _deactivateHandler;
    private readonly ILogger<AdminEmployeesController> _logger;

    public AdminEmployeesController(
        GetAdminEmployeesHandler getHandler,
        GetAdminEmployeeByIdHandler getByIdHandler,
        CreateAdminEmployeeHandler createHandler,
        UpdateAdminEmployeeHandler updateHandler,
        ActivateAdminEmployeeHandler activateHandler,
        DeactivateAdminEmployeeHandler deactivateHandler,
        ILogger<AdminEmployeesController> logger)
    {
        _getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        _getByIdHandler = getByIdHandler ?? throw new ArgumentNullException(nameof(getByIdHandler));
        _createHandler = createHandler ?? throw new ArgumentNullException(nameof(createHandler));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _activateHandler = activateHandler ?? throw new ArgumentNullException(nameof(activateHandler));
        _deactivateHandler = deactivateHandler ?? throw new ArgumentNullException(nameof(deactivateHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminEmployeesQuery { CurrentUserId = currentUserId, Search = search, IsActive = isActive };
        var result = await _getHandler.Handle(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminEmployeeByIdQuery { CurrentUserId = currentUserId, Id = id };
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
    [Authorize(Policy = "Admin.Security.EditEmployees")]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] AdminEmployeeCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new CreateAdminEmployeeCommand
        {
            CurrentUserId = currentUserId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        };

        try
        {
            var dto = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/admin/security/employees/{dto.Id}", dto);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin.Security.EditEmployees")]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, [FromBody] AdminEmployeeUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new UpdateAdminEmployeeCommand
        {
            CurrentUserId = currentUserId,
            Id = id,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
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
    [Authorize(Policy = "Admin.Security.EditEmployees")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ActivateAdminEmployeeCommand { CurrentUserId = currentUserId, Id = id };
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
    [Authorize(Policy = "Admin.Security.EditEmployees")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new DeactivateAdminEmployeeCommand { CurrentUserId = currentUserId, Id = id };
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

