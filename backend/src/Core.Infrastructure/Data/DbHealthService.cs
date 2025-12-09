using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MyIS.Core.Infrastructure.Data;

public sealed class DbHealthService : IDbHealthService
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DbHealthService> _logger;

    public DbHealthService(
        IConnectionStringProvider connectionStringProvider,
        IHostEnvironment hostEnvironment,
        ILogger<DbHealthService> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<DbConnectionStatus> CheckConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var result = _connectionStringProvider.GetDefaultConnection();
        var environmentName = _hostEnvironment.EnvironmentName ?? string.Empty;

        if (!result.IsConfigured || string.IsNullOrWhiteSpace(result.ConnectionString))
        {
            return new DbConnectionStatus
            {
                Configured = false,
                CanConnect = false,
                LastError = "Connection string 'Default' is not configured.",
                Environment = environmentName,
                ConnectionStringSource = result.Source,
                RawSourceDescription = result.RawSourceDescription
            };
        }

        try
        {
            await using var connection = new NpgsqlConnection(result.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return new DbConnectionStatus
            {
                Configured = true,
                CanConnect = true,
                LastError = null,
                Environment = environmentName,
                ConnectionStringSource = result.Source,
                RawSourceDescription = result.RawSourceDescription
            };
        }
        catch (OperationCanceledException)
        {
            const string message = "Database connection check was canceled.";

            _logger.LogWarning("Database connection check was canceled.");

            return new DbConnectionStatus
            {
                Configured = result.IsConfigured,
                CanConnect = false,
                LastError = message,
                Environment = environmentName,
                ConnectionStringSource = result.Source,
                RawSourceDescription = result.RawSourceDescription
            };
        }
        catch (Exception ex)
        {
            var safeMessage = $"{ex.GetType().Name}: {ex.Message}";

            _logger.LogError(ex, "Database connection check failed: {Error}", safeMessage);

            return new DbConnectionStatus
            {
                Configured = result.IsConfigured,
                CanConnect = false,
                LastError = safeMessage,
                Environment = environmentName,
                ConnectionStringSource = result.Source,
                RawSourceDescription = result.RawSourceDescription
            };
        }
    }
}