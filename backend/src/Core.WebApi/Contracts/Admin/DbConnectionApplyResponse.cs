namespace MyIS.Core.WebApi.Contracts.Admin;

public sealed class DbConnectionApplyResponse
{
    public bool Applied { get; init; }

    public bool CanConnect { get; init; }

    public bool MigrationsApplied { get; init; }

    public string? LastError { get; init; }

    public string Environment { get; init; } = string.Empty;

    public DbConnectionSource ConnectionStringSource { get; init; }

    public string? SourceDescription { get; init; }

    /// <summary>
    /// Безопасное описание подключения (без пароля и полной строки).
    /// </summary>
    public string? SafeConnectionInfo { get; init; }
}