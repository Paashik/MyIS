namespace MyIS.Core.Infrastructure.Data;

public interface IConnectionStringProvider
{
    /// <summary>
    /// Возвращает информацию о строке подключения по умолчанию.
   /// Никогда не выбрасывает исключения наружу.
    /// </summary>
    ConnectionStringResult GetDefaultConnection();

    /// <summary>
    /// Пытается получить строку подключения по умолчанию.
    /// </summary>
    /// <param name="connectionString">Строка подключения (если найдена).</param>
    /// <param name="source">Источник строки подключения.</param>
    /// <param name="rawSourceDescription">
    /// Текстовое описание источника / состояния без секретов.
    /// </param>
    /// <returns>true, если строка подключения сконфигурирована.</returns>
    bool TryGetDefaultConnectionString(
        out string? connectionString,
        out ConnectionStringSource source,
        out string? rawSourceDescription);
}