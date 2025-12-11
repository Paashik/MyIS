using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Queries;

namespace MyIS.Core.WebApi.Controllers.Requests;

[ApiController]
[Route("api/request-types")]
[Authorize]
public sealed class RequestTypesController : ControllerBase
{
    private readonly GetRequestTypesHandler _handler;
    private readonly ILogger<RequestTypesController> _logger;

    public RequestTypesController(
        GetRequestTypesHandler handler,
        ILogger<RequestTypesController> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Справочник типов заявок.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RequestTypeDto>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var query = new GetRequestTypesQuery
        {
            CurrentUserId = currentUserId
        };

        var result = await _handler.Handle(query, cancellationToken);
        return Ok(result.Items);
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