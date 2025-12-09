namespace MyIS.Core.WebApi.Contracts.Admin;

public sealed class DbConnectionConfigRequest
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }

    public string Database { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool UseSsl { get; init; }

    public bool TrustServerCertificate { get; init; }

    public int? TimeoutSeconds { get; init; }
}