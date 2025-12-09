using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi.Contracts.Admin;
using Npgsql;

namespace MyIS.Core.WebApi.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminDbController : ControllerBase
{
    private readonly IDbHealthService _dbHealthService;
    private readonly IHostEnvironment _environment;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminDbController> _logger;

    public AdminDbController(
        IDbHealthService dbHealthService,
        IHostEnvironment environment,
        IServiceScopeFactory scopeFactory,
        ILogger<AdminDbController> logger)
    {
        _dbHealthService = dbHealthService;
        _environment = environment;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Текущий статус конфигурации и доступности БД.
    /// </summary>
    [HttpGet("db-status")]
    public async Task<ActionResult<DbConnectionStatus>> GetDbStatus(CancellationToken cancellationToken)
    {
        var status = await _dbHealthService.CheckConnectionAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Проверка произвольной конфигурации подключения без сохранения.
    /// </summary>
    [HttpPost("db-config/test")]
    public async Task<ActionResult<DbConnectionTestResponse>> TestConnection(
        [FromBody] DbConnectionConfigRequest request,
        CancellationToken cancellationToken)
    {
        var builder = BuildConnectionString(request);
        var connectionString = builder.ConnectionString;
        var safeInfo = BuildSafeInfo(builder);
        var envName = _environment.EnvironmentName ?? string.Empty;

        var canConnect = false;
        string? lastError = null;

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            canConnect = true;
        }
        catch (OperationCanceledException)
        {
            const string msg = "Database connection test was canceled.";
            _logger.LogWarning("Database connection test was canceled.");
            lastError = msg;
        }
        catch (Exception ex)
        {
            var safeMsg = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "Database connection test failed: {Error}", safeMsg);
            lastError = safeMsg;
        }

        var response = new DbConnectionTestResponse
        {
            Configured = true,
            CanConnect = canConnect,
            LastError = lastError,
            Environment = envName,
            ConnectionStringSource = ConnectionStringSource.Configuration,
            SourceDescription = "Temporary connection string from API request.",
            SafeConnectionInfo = safeInfo
        };

        return Ok(response);
    }

    /// <summary>
    /// Применение конфигурации подключения, сохранение в appsettings.Local.json
    /// и прогон миграций. Доступно только в окружении Development.
    /// </summary>
    [HttpPost("db-config/apply")]
    public async Task<ActionResult<DbConnectionApplyResponse>> ApplyConnection(
        [FromBody] DbConnectionConfigRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return Forbid();
        }

        var builder = BuildConnectionString(request);
        var connectionString = builder.ConnectionString;
        var safeInfo = BuildSafeInfo(builder);
        var envName = _environment.EnvironmentName ?? string.Empty;

        var applied = false;
        var canConnect = false;
        var migrationsApplied = false;
        string? lastError = null;

        // 1. Сохранение строки подключения в appsettings.Local.json
        try
        {
            WriteLocalConnectionString(connectionString);
            applied = true;
        }
        catch (Exception ex)
        {
            var safeMsg = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "Failed to persist connection string to appsettings.Local.json: {Error}", safeMsg);
            lastError = AppendError(lastError, safeMsg);
        }

        // 2. Проверка подключения по только что сформированной строке
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            canConnect = true;
        }
        catch (OperationCanceledException)
        {
            const string msg = "Database connection apply test was canceled.";
            _logger.LogWarning("Database connection apply test was canceled.");
            lastError = AppendError(lastError, msg);
        }
        catch (Exception ex)
        {
            var safeMsg = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "Database connection apply test failed: {Error}", safeMsg);
            lastError = AppendError(lastError, safeMsg);
        }

        // 3. Прогон миграций, только если подключение успешно
        if (canConnect)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await dbContext.Database.MigrateAsync(cancellationToken);
                migrationsApplied = true;
            }
            catch (OperationCanceledException)
            {
                const string msg = "Applying migrations was canceled.";
                _logger.LogWarning("Applying migrations was canceled.");
                lastError = AppendError(lastError, msg);
            }
            catch (Exception ex)
            {
                var safeMsg = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogError(ex, "Failed to apply migrations: {Error}", safeMsg);
                migrationsApplied = false;
                lastError = AppendError(lastError, safeMsg);
            }
        }

        var response = new DbConnectionApplyResponse
        {
            Applied = applied,
            CanConnect = canConnect,
            MigrationsApplied = migrationsApplied,
            LastError = lastError,
            Environment = envName,
            ConnectionStringSource = ConnectionStringSource.AppSettingsLocal,
            SourceDescription = "appsettings.Local.json: ConnectionStrings:Default (written via Admin API).",
            SafeConnectionInfo = safeInfo
        };

        return Ok(response);
    }

    private void WriteLocalConnectionString(string connectionString)
    {
        var contentRoot = _environment.ContentRootPath;
        var path = Path.Combine(contentRoot, "appsettings.Local.json");

        JsonNode rootNode;

        if (System.IO.File.Exists(path))
        {
            using var stream = System.IO.File.OpenRead(path);
            rootNode = JsonNode.Parse(stream) ?? new JsonObject();
        }
        else
        {
            rootNode = new JsonObject();
        }

        if (rootNode is not JsonObject rootObject)
        {
            rootObject = new JsonObject();
        }

        if (rootObject["ConnectionStrings"] is not JsonObject connStrings)
        {
            connStrings = new JsonObject();
            rootObject["ConnectionStrings"] = connStrings;
        }

        connStrings["Default"] = connectionString;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        using var writeStream = System.IO.File.Create(path);
        JsonSerializer.Serialize(writeStream, rootObject, options);
    }

    private static NpgsqlConnectionStringBuilder BuildConnectionString(DbConnectionConfigRequest request)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = request.Host,
            Database = request.Database,
            Username = request.Username,
            Password = request.Password
        };

        if (request.Port > 0)
        {
            builder.Port = request.Port;
        }

        if (request.TimeoutSeconds.HasValue && request.TimeoutSeconds.Value > 0)
        {
            builder.Timeout = request.TimeoutSeconds.Value;
        }

        if (request.UseSsl)
        {
            builder.SslMode = SslMode.Require;
            builder.TrustServerCertificate = request.TrustServerCertificate;
        }
        else
        {
            builder.SslMode = SslMode.Disable;
        }

        return builder;
    }

    private static string BuildSafeInfo(NpgsqlConnectionStringBuilder builder)
    {
        return
            $"Host={builder.Host}; Port={builder.Port}; Database={builder.Database}; Username={builder.Username}; SSL Mode={builder.SslMode}; Timeout={builder.Timeout}; Pooling={builder.Pooling}; TrustServerCertificate={builder.TrustServerCertificate}";
    }

    private static string AppendError(string? existing, string next)
    {
        if (string.IsNullOrWhiteSpace(existing))
        {
            return next;
        }

        return existing + " | " + next;
    }
}