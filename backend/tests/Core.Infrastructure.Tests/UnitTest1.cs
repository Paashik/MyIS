using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Tests;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";

    public string ApplicationName { get; set; } = "MyIS.Tests";

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    // В тестах ContentRootFileProvider не используется, поэтому можно не реализовывать.
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}

public sealed class FakeConnectionStringProvider : IConnectionStringProvider
{
    private readonly ConnectionStringResult _result;

    public FakeConnectionStringProvider(ConnectionStringResult result)
    {
        _result = result;
    }

    public ConnectionStringResult GetDefaultConnection() => _result;

    public bool TryGetDefaultConnectionString(
        out string? connectionString,
        out ConnectionStringSource source,
        out string? rawSourceDescription)
    {
        connectionString = _result.ConnectionString;
        source = _result.Source;
        rawSourceDescription = _result.RawSourceDescription;

        return _result.IsConfigured && !string.IsNullOrWhiteSpace(_result.ConnectionString);
    }
}

public class DefaultConnectionStringProviderTests
{
    [Fact]
    public void When_Local_Appsettings_Has_Default_Connection_It_Takes_Priority_Over_Configuration()
    {
        // Arrange
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "MyIS_DefaultConnectionStringProviderTests_" + Guid.NewGuid());

        Directory.CreateDirectory(tempDir);

        try
        {
            var localJsonPath = Path.Combine(tempDir, "appsettings.Local.json");

            var json = """
            {
              "ConnectionStrings": {
                "Default": "Host=local;Port=5432;Database=localdb;Username=u;Password=p"
              }
            }
            """;
            File.WriteAllText(localJsonPath, json);

            var inMemory = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=config;Port=5432;Database=configdb;Username=u;Password=p"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var env = new TestHostEnvironment
            {
                ContentRootPath = tempDir
            };

            var provider = new DefaultConnectionStringProvider(configuration, env);

            // Act
            var result = provider.GetDefaultConnection();

            // Assert
            Assert.True(result.IsConfigured);
            Assert.Equal(ConnectionStringSource.AppSettingsLocal, result.Source);
            Assert.Equal(
                "Host=local;Port=5432;Database=localdb;Username=u;Password=p",
                result.ConnectionString);
            Assert.NotNull(result.RawSourceDescription);
            Assert.Contains("appsettings.Local.json", result.RawSourceDescription);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // тестам не критично, если временную папку не удалось удалить
            }
        }
    }

    [Fact]
    public void When_Local_File_Is_Absent_But_Configuration_Has_Connection_String_It_Is_Used()
    {
        // Arrange
        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "MyIS_DefaultConnectionStringProviderTests_NoLocal_" + Guid.NewGuid());

        Directory.CreateDirectory(tempDir);

        try
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=config;Port=5432;Database=configdb;Username=u;Password=p"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            var env = new TestHostEnvironment
            {
                ContentRootPath = tempDir
            };

            var provider = new DefaultConnectionStringProvider(configuration, env);

            // Act
            var result = provider.GetDefaultConnection();

            // Assert
            Assert.True(result.IsConfigured);
            Assert.Equal(ConnectionStringSource.Configuration, result.Source);
            Assert.Equal(
                "Host=config;Port=5432;Database=configdb;Username=u;Password=p",
                result.ConnectionString);
            Assert.NotNull(result.RawSourceDescription);
            Assert.Contains("Configuration.GetConnectionString", result.RawSourceDescription);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void When_No_Connection_String_Configured_Result_Is_Not_Configured_With_Details()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var env = new TestHostEnvironment
        {
            ContentRootPath = Path.Combine(
                Path.GetTempPath(),
                "MyIS_DefaultConnectionStringProviderTests_Empty_" + Guid.NewGuid())
        };

        var provider = new DefaultConnectionStringProvider(configuration, env);

        // Act
        var result = provider.GetDefaultConnection();

        // Assert
        Assert.False(result.IsConfigured);
        Assert.Null(result.ConnectionString);
        Assert.Equal(ConnectionStringSource.None, result.Source);
        Assert.NotNull(result.RawSourceDescription);
        Assert.Contains("not configured", result.RawSourceDescription!, StringComparison.OrdinalIgnoreCase);
    }
}

public class DbHealthServiceTests
{
    [Fact]
    public async Task When_Connection_Is_Not_Configured_Returns_Configured_False_And_CanConnect_False()
    {
        // Arrange
        var connectionInfo = new ConnectionStringResult
        {
            IsConfigured = false,
            ConnectionString = null,
            Source = ConnectionStringSource.None,
            RawSourceDescription = "not configured"
        };

        var provider = new FakeConnectionStringProvider(connectionInfo);
        var env = new TestHostEnvironment
        {
            EnvironmentName = "Development"
        };

        using var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<DbHealthService>();

        var service = new DbHealthService(provider, env, logger);

        // Act
        var status = await service.CheckConnectionAsync();

        // Assert
        Assert.False(status.Configured);
        Assert.False(status.CanConnect);
        Assert.Equal("Connection string 'Default' is not configured.", status.LastError);
        Assert.Equal("Development", status.Environment);
        Assert.Equal(ConnectionStringSource.None, status.ConnectionStringSource);
        Assert.Equal("not configured", status.RawSourceDescription);
    }

    [Fact]
    public async Task When_Connection_String_Is_Invalid_Returns_Configured_True_And_CanConnect_False()
    {
        // Arrange
        var connectionInfo = new ConnectionStringResult
        {
            IsConfigured = true,
            ConnectionString = "Host=invalid;Port=1;Database=test;Username=u;Password=p",
            Source = ConnectionStringSource.Configuration,
            RawSourceDescription = "invalid test connection string"
        };

        var provider = new FakeConnectionStringProvider(connectionInfo);
        var env = new TestHostEnvironment
        {
            EnvironmentName = "Test"
        };

        using var loggerFactory = LoggerFactory.Create(builder => { });
        var logger = loggerFactory.CreateLogger<DbHealthService>();

        var service = new DbHealthService(provider, env, logger);

        // Act
        var status = await service.CheckConnectionAsync();

        // Assert
        Assert.True(status.Configured);
        Assert.False(status.CanConnect);
        Assert.Equal("Test", status.Environment);
        Assert.Equal(ConnectionStringSource.Configuration, status.ConnectionStringSource);
        Assert.Equal("invalid test connection string", status.RawSourceDescription);
        Assert.False(string.IsNullOrWhiteSpace(status.LastError));
    }
}
