using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса на получение версий BOM
/// </summary>
public class GetEbomVersionsHandler : IRequestHandler<GetEbomVersionsQuery, EbomVersionsResponse>
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IProductRepository _productRepository;

    public GetEbomVersionsHandler(IBomVersionRepository bomVersionRepository, IProductRepository productRepository)
    {
        _bomVersionRepository = bomVersionRepository;
        _productRepository = productRepository;
    }

    public async Task<EbomVersionsResponse> Handle(GetEbomVersionsQuery request, CancellationToken cancellationToken)
    {
        // Find product by itemId
        var product = await _productRepository.GetByItemIdAsync(request.ItemId, cancellationToken);
        if (product == null)
        {
            return new EbomVersionsResponse(new List<EbomVersionDto>());
        }

        var versions = await _bomVersionRepository.GetByProductIdAsync(product.Id, cancellationToken);

        var dtos = versions.Select(v => new EbomVersionDto(
            v.Id,
            request.ItemId, // Return the original itemId for UI compatibility
            v.VersionCode,
            v.Status.ToString(),
            v.Source.ToString(),
            v.UpdatedAt
        )).ToList();

        return new EbomVersionsResponse(dtos);
    }
}