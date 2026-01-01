using MediatR;

namespace MyIS.Core.Application.Engineering.Commands;

/// <summary>
/// Команда удаления строки BOM
/// </summary>
public record DeleteEbomLineCommand(Guid LineId) : IRequest<DeleteEbomLineResponse>;

/// <summary>
/// Ответ на удаление строки BOM
/// </summary>
public record DeleteEbomLineResponse(bool Success);