using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на получение списка изделий для eBOM (для экрана EngineeringPage).
/// </summary>
public record GetEbomProductsQuery(
    string? Search = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 200) : IRequest<EbomProductsResponse>;

/// <summary>
/// Ответ со списком изделий для eBOM.
/// Важно: WebApi отдаёт наружу массив (response.Products), а не объект-обёртку.
/// </summary>
public record EbomProductsResponse(IReadOnlyList<EbomProductListItemDto> Products);

/// <summary>
/// DTO строки списка изделий для eBOM.
/// </summary>
public record EbomProductListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    Guid ItemId,
    bool HasBomVersions,
    int BomVersionsCount,
    DateTimeOffset UpdatedAt
);