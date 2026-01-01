using MediatR;

namespace MyIS.Core.Application.Engineering.Commands;

/// <summary>
/// Команда импорта EBOM из файла Access
/// </summary>
public record ImportEbomFromAccessCommand(
    string FilePath,
    Guid ProductId) : IRequest<ImportEbomFromAccessResponse>;

/// <summary>
/// Ответ на импорт EBOM из Access
/// </summary>
public record ImportEbomFromAccessResponse(
    int ImportedLinesCount,
    int SkippedLinesCount,
    List<MissingItemInfo> MissingItems,
    int SkippedBomsCount,
    List<SkippedBomInfo> SkippedBoms,
    string Message);

/// <summary>
/// Информация о пропущенной позиции
/// </summary>
public record MissingItemInfo(
    string AccessId,
    string Name,
    string Reason);

/// <summary>
/// Информация о пропущенной BOM версии
/// </summary>
public record SkippedBomInfo(
    int BomId,
    int ProductId,
    string VersionCode,
    string Reason);