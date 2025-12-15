using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MyIS.Core.WebApi.Middleware;

/// <summary>
/// Перехватывает UnauthorizedAccessException из Application/Domain и конвертирует её в единообразный HTTP 403.
/// </summary>
public sealed class UnauthorizedAccessExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnauthorizedAccessExceptionMiddleware> _logger;

    public UnauthorizedAccessExceptionMiddleware(
        RequestDelegate next,
        ILogger<UnauthorizedAccessExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex) when (!context.Response.HasStarted)
        {
            _logger.LogWarning(ex, "Access denied: {Message}", ex.Message);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
