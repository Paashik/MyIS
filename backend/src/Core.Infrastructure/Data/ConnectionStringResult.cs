namespace MyIS.Core.Infrastructure.Data;

public sealed class ConnectionStringResult
{
    public bool IsConfigured { get; init; }

    public string? ConnectionString { get; init; }

    public ConnectionStringSource Source { get; init; }

    /// <summary>
    /// Текстовое описание источника / состояния, безопасное для логов и UI.
    /// Не должно содержать секретов и полных строк подключения.
    /// </summary>
    public string? RawSourceDescription { get; init; }
}