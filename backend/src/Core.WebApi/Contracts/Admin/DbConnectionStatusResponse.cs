using System.Text.Json.Serialization;

namespace MyIS.Core.WebApi.Contracts.Admin;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DbConnectionSource
{
    Unknown = 0,
    Configuration = 1,
    Environment = 2,
    AppSettingsLocal = 3
}

public sealed class DbConnectionStatusResponse
{
    public bool Configured { get; init; }

    public bool CanConnect { get; init; }

    public string? LastError { get; init; }

    public string Environment { get; init; } = string.Empty;

    public DbConnectionSource ConnectionStringSource { get; init; }

    public string? RawSourceDescription { get; init; }
}