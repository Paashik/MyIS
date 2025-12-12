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
[Route("api/admin/requests/workflow")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
[Authorize(Policy = "Admin.Requests.EditWorkflow")]
public sealed class AdminRequestWorkflowController : ControllerBase
{
    private readonly GetAdminRequestWorkflowTransitionsHandler _getHandler;
    private readonly ReplaceAdminRequestWorkflowTransitionsHandler _replaceHandler;
    private readonly ILogger<AdminRequestWorkflowController> _logger;

    public AdminRequestWorkflowController(
        GetAdminRequestWorkflowTransitionsHandler getHandler,
        ReplaceAdminRequestWorkflowTransitionsHandler replaceHandler,
        ILogger<AdminRequestWorkflowController> logger)
    {
        _getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        _replaceHandler = replaceHandler ?? throw new ArgumentNullException(nameof(replaceHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("transitions")]
    public async Task<ActionResult<IReadOnlyList<RequestWorkflowTransitionDto>>> GetTransitions(
        [FromQuery] string? typeCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var query = new GetAdminRequestWorkflowTransitionsQuery
        {
            CurrentUserId = currentUserId,
            TypeCode = typeCode
        };

        try
        {
            var result = await _getHandler.Handle(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("transitions")]
    public async Task<IActionResult> ReplaceTransitions(
        [FromBody] AdminReplaceWorkflowTransitionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

        var command = new ReplaceAdminRequestWorkflowTransitionsCommand
        {
            CurrentUserId = currentUserId,
            TypeCode = request.TypeCode,
            Transitions = Array.ConvertAll(request.Transitions, x => new RequestWorkflowTransitionInputDto
            {
                FromStatusId = x.FromStatusId,
                ToStatusId = x.ToStatusId,
                ActionCode = x.ActionCode,
                RequiredPermission = x.RequiredPermission,
                IsEnabled = x.IsEnabled
            })
        };

        try
        {
            await _replaceHandler.Handle(command, cancellationToken);
            return NoContent();
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

