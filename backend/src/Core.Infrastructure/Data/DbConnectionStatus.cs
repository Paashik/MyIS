namespace MyIS.Core.Infrastructure.Data;

public sealed class DbConnectionStatus
{
    /// <summary>
    /// Конфигурация строки подключения присутствует.
    /// </summary>
    public bool Configured { get; init; }

    /// <summary>
    /// Удалось подключиться к БД с текущей конфигурацией.
    /// </summary>
    public bool CanConnect { get; init; }

    /// <summary>
    /// Последняя ошибка (без секретов), если подключение не удалось.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Имя окружения ASP.NET Core (Development/Staging/Production/...).
    /// </summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>
    /// Источник строки подключения.
    /// </summary>
    public ConnectionStringSource ConnectionStringSource { get; init; }

    /// <summary>
    /// Безопасное текстовое описание источника / состояния (без пароля).
    /// </summary>
    public string? RawSourceDescription { get; init; }
}