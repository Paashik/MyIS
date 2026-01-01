namespace MyIS.Core.WebApi.Contracts.Engineering;

/// <summary>
/// Запрос на импорт EBOM из файла Access
/// </summary>
public record ImportEbomFromAccessRequest(
    string FilePath,
    Guid ProductId);

/// <summary>
/// Ответ на импорт EBOM из Access
/// </summary>
public record ImportEbomFromAccessResponse(
    int ImportedLinesCount,
    string Message);