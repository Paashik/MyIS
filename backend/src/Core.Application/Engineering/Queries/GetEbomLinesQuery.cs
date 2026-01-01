using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на получение строк BOM
/// </summary>
public record GetEbomLinesQuery(
    Guid BomVersionId,
    Guid ParentItemId,
    bool OnlyErrors = false) : IRequest<EbomLinesResponse>;

/// <summary>
/// Ответ со строками BOM
/// </summary>
public record EbomLinesResponse(IReadOnlyList<EbomLineDto> Lines);