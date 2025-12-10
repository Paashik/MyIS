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
        var connectionInfo = _connectionStringProvider.GetDefaultConnection();
        var environmentName = _hostEnvironment.EnvironmentName ?? string.Empty;

        if (!connectionInfo.IsConfigured || string.IsNullOrWhiteSpace(connectionInfo.ConnectionString))
        {
            return new DbConnectionStatus
            {
                Configured = false,
                CanConnect = false,
                LastError = "Connection string 'Default' is not configured.",
                Environment = environmentName,
                ConnectionStringSource = connectionInfo.Source,
                RawSourceDescription = connectionInfo.RawSourceDescription
            };
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            return new DbConnectionStatus
            {
                Configured = true,
                CanConnect = true,
                LastError = null,
                Environment = environmentName,
                ConnectionStringSource = connectionInfo.Source,
                RawSourceDescription = connectionInfo.RawSourceDescription
            };
        }
        catch (OperationCanceledException)
        {
            const string message = "Database connection check was canceled.";

            _logger.LogWarning("Database connection check was canceled.");

            return new DbConnectionStatus
            {
                Configured = connectionInfo.IsConfigured,
                CanConnect = false,
                LastError = message,
                Environment = environmentName,
                ConnectionStringSource = connectionInfo.Source,
                RawSourceDescription = connectionInfo.RawSourceDescription
            };
        }
        catch (Exception ex)
        {
            var safeMessage = ex.Message;

            _logger.LogWarning(ex, "Failed to connect to PostgreSQL database: {Error}", safeMessage);

            return new DbConnectionStatus
            {
                Configured = true,
                CanConnect = false,
                LastError = safeMessage,
                Environment = environmentName,
                ConnectionStringSource = connectionInfo.Source,
                RawSourceDescription = connectionInfo.RawSourceDescription
            };
        }
    }
}