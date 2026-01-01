using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на "взрыв" спецификации (получение полного состава версии eBOM).
/// </summary>
/// <param name="BomVersionId">Идентификатор версии BOM.</param>
/// <param name="MaxDepth">Лимит глубины обхода (защита от циклов).</param>
/// <param name="MaxRows">Лимит количества строк результата (защита от больших BOM).</param>
public sealed record GetEbomExplosionQuery(
    Guid BomVersionId,
    int MaxDepth = 64,
    int MaxRows = 20000) : IRequest<EbomExplosionResponse>;

/// <summary>
/// Ответ с полным составом eBOM версии (плоский список строк со служебными полями Path/Level/TotalQty).
/// </summary>
public sealed record EbomExplosionResponse(
    Guid RootItemId,
    IReadOnlyList<EbomExplosionRowDto> Rows);

/// <summary>
/// Строка результата "взрыва" eBOM.
/// </summary>
public sealed record EbomExplosionRowDto(
    Guid LineId,
    Guid ParentItemId,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string Role,
    decimal Qty,
    decimal TotalQty,
    string UomCode,
    string? PositionNo,
    string? Notes,
    string LineStatus,
    int Level,
    string Path);