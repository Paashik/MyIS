using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MyIS.Core.Infrastructure.Data;

public sealed class DefaultConnectionStringProvider : IConnectionStringProvider
{
    private const string DefaultConnectionName = "Default";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public DefaultConnectionStringProvider(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public ConnectionStringResult GetDefaultConnection()
    {
        try
        {
            // 1. Явное чтение appsettings.Local.json с приоритетом
            var localResult = TryReadFromLocalFile();
            if (localResult.IsConfigured)
            {
                return localResult;
            }

            // 2. Обычная конфигурация (appsettings.json, env vars, user-secrets и т.п.)
            var fromConfig = _configuration.GetConnectionString(DefaultConnectionName);

            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return new ConnectionStringResult
                {
                    IsConfigured = true,
                    ConnectionString = fromConfig,
                    Source = ConnectionStringSource.Configuration,
                    RawSourceDescription = BuildSourceDescription(
                        $"Configuration.GetConnectionString(\"{DefaultConnectionName}\")")
                };
            }

            return new ConnectionStringResult
            {
                IsConfigured = false,
                ConnectionString = null,
                Source = ConnectionStringSource.None,
                RawSourceDescription = "Connection string 'Default' is not configured in any known source."
            };
        }
        catch (Exception ex)
        {
            // Никаких исключений наружу — только безопасное текстовое описание
            return new ConnectionStringResult
            {
                IsConfigured = false,
                ConnectionString = null,
                Source = ConnectionStringSource.None,
                RawSourceDescription =
                    $"Error while resolving connection string: {ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    public bool TryGetDefaultConnectionString(
        out string? connectionString,
        out ConnectionStringSource source,
        out string? rawSourceDescription)
    {
        var result = GetDefaultConnection();

        connectionString = result.ConnectionString;
        source = result.Source;
        rawSourceDescription = result.RawSourceDescription;

        return result.IsConfigured && !string.IsNullOrWhiteSpace(result.ConnectionString);
    }

    private ConnectionStringResult TryReadFromLocalFile()
    {
        try
        {
            var contentRoot = _hostEnvironment.ContentRootPath;
            var localPath = Path.Combine(contentRoot, "appsettings.Local.json");

            if (!File.Exists(localPath))
            {
                return new ConnectionStringResult
                {
                    IsConfigured = false,
                    ConnectionString = null,
                    Source = ConnectionStringSource.None,
                    RawSourceDescription = "appsettings.Local.json not found."
                };
            }

            using var stream = File.OpenRead(localPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connSection) ||
                !connSection.TryGetProperty(DefaultConnectionName, out var defaultConnProp) ||
                defaultConnProp.ValueKind != JsonValueKind.String)
            {
                return new ConnectionStringResult
                {
                    IsConfigured = false,
                    ConnectionString = null,
                    Source = ConnectionStringSource.AppSettingsLocal,
                    RawSourceDescription =
                        "appsettings.Local.json does not contain ConnectionStrings:Default as a string."
                };
            }

            var connectionString = defaultConnProp.GetString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return new ConnectionStringResult
                {
                    IsConfigured = false,
                    ConnectionString = null,
                    Source = ConnectionStringSource.AppSettingsLocal,
                    RawSourceDescription =
                        "appsettings.Local.json has empty ConnectionStrings:Default."
                };
            }

            return new ConnectionStringResult
            {
                IsConfigured = true,
                ConnectionString = connectionString,
                Source = ConnectionStringSource.AppSettingsLocal,
                RawSourceDescription = BuildSourceDescription(
                    "appsettings.Local.json: ConnectionStrings:Default")
            };
        }
        catch (Exception ex)
        {
            return new ConnectionStringResult
            {
                IsConfigured = false,
                ConnectionString = null,
                Source = ConnectionStringSource.AppSettingsLocal,
                RawSourceDescription =
                    $"Failed to read appsettings.Local.json: {ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    private static string BuildSourceDescription(string prefix)
    {
        return $"{prefix} (value hidden)";
    }
}
