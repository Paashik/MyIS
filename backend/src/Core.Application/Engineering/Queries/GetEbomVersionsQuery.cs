using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на получение списка версий BOM для изделия
/// </summary>
public record GetEbomVersionsQuery(Guid ItemId) : IRequest<EbomVersionsResponse>;

/// <summary>
/// Ответ с версиями BOM
/// </summary>
public record EbomVersionsResponse(IReadOnlyList<EbomVersionDto> Versions);