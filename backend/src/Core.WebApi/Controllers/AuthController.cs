using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Auth;
using MyIS.Core.WebApi.Contracts.Auth;

namespace MyIS.Core.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new AuthErrorResponse
            {
                Code = "INVALID_REQUEST",
                Message = "Request body is required."
            });
        }

        var result = await _authService.LoginAsync(request.Login, request.Password, cancellationToken);

        if (!result.Success)
        {
            if (string.Equals(result.ErrorCode, "INVALID_CREDENTIALS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Failed login attempt for login {Login}: invalid credentials.", request.Login);

                return Unauthorized(new AuthErrorResponse
                {
                    Code = "INVALID_CREDENTIALS",
                    Message = "Неверный логин или пароль"
                });
            }

            if (string.Equals(result.ErrorCode, "USER_INACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Failed login attempt for login {Login}: user inactive.", request.Login);

                return StatusCode(StatusCodes.Status403Forbidden, new AuthErrorResponse
                {
                    Code = "USER_BLOCKED",
                    Message = "Учетная запись заблокирована"
                });
            }

            _logger.LogError(
                "Authentication failed for login {Login} with unexpected error code {ErrorCode}.",
                request.Login,
                result.ErrorCode);

            return StatusCode(StatusCodes.Status500InternalServerError, new AuthErrorResponse
            {
                Code = "AUTH_ERROR",
                Message = "Ошибка аутентификации"
            });
        }

        var principal = CreatePrincipalFromAuthResult(result);
        await HttpContext.SignInAsync("Cookies", principal);

        _logger.LogInformation("User {UserId} logged in successfully.", result.UserId);

        var userDto = MapToUserDto(result);

        var response = new AuthLoginResponse
        {
            User = userDto
        };

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await HttpContext.SignOutAsync("Cookies");

        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} logged out.", userId);
        }
        else
        {
            _logger.LogInformation("Logout called for anonymous user.");
        }

        var response = new AuthLogoutResponse
        {
            Message = "Выход выполнен"
        };

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new AuthErrorResponse
            {
                Code = "UNAUTHORIZED",
                Message = "Требуется аутентификация"
            });
        }

        var result = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (result is null || !result.Success)
        {
            return Unauthorized(new AuthErrorResponse
            {
                Code = "UNAUTHORIZED",
                Message = "Требуется аутентификация"
            });
        }

        var userDto = MapToUserDto(result);
        return Ok(userDto);
    }

    private static ClaimsPrincipal CreatePrincipalFromAuthResult(AuthResult result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId!.Value.ToString()),
            new(ClaimTypes.Name, result.Login!)
        };

        if (result.Roles is { Count: > 0 })
        {
            claims.AddRange(result.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(identity);
    }

    private static AuthUserDto MapToUserDto(AuthResult result)
    {
        return new AuthUserDto
        {
            Id = result.UserId!.Value,
            Login = result.Login!,
            FullName = result.FullName ?? string.Empty,
            Roles = result.Roles
        };
    }
}