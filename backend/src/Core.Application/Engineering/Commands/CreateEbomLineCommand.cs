using MediatR;

namespace MyIS.Core.Application.Engineering.Commands;

/// <summary>
/// Команда создания строки BOM
/// </summary>
public record CreateEbomLineCommand(
    Guid BomVersionId,
    Guid ParentItemId,
    Guid ItemId,
    string Role,
    decimal Qty,
    string? PositionNo = null,
    string? Notes = null) : IRequest<CreateEbomLineResponse>;

/// <summary>
/// Ответ на создание строки BOM
/// </summary>
public record CreateEbomLineResponse(EbomLineDto Line);