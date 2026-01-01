using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса списка изделий для eBOM.
/// </summary>
public sealed class GetEbomProductsHandler : IRequestHandler<GetEbomProductsQuery, EbomProductsResponse>
{
    private readonly IProductRepository _productRepository;

    public GetEbomProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<EbomProductsResponse> Handle(GetEbomProductsQuery request, CancellationToken cancellationToken)
    {
        ProductType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type) &&
            Enum.TryParse<ProductType>(request.Type, ignoreCase: true, out var parsed))
        {
            type = parsed;
        }

        var (items, _totalCount) = await _productRepository.SearchAsync(
            searchTerm: request.Search,
            type: type,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var dtos = items
            .Select(p => new EbomProductListItemDto(
                Id: p.Id,
                Code: p.Code,
                Name: p.Name,
                Description: p.Description,
                Type: p.Type.ToString(),
                ItemId: p.ItemId,
                HasBomVersions: p.BomVersions.Count > 0,
                BomVersionsCount: p.BomVersions.Count,
                UpdatedAt: p.UpdatedAt
            ))
            .ToList();

        return new EbomProductsResponse(dtos);
    }
}