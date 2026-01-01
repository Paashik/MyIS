using MediatR;

namespace MyIS.Core.Application.Engineering.Commands;

/// <summary>
/// Команда обновления строки BOM
/// </summary>
public record UpdateEbomLineCommand(
    Guid LineId,
    string? Role = null,
    decimal? Qty = null,
    string? PositionNo = null,
    string? Notes = null,
    Guid? ItemId = null) : IRequest<UpdateEbomLineResponse>;

/// <summary>
/// Ответ на обновление строки BOM
/// </summary>
public record UpdateEbomLineResponse(EbomLineDto Line);