using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса на получение версии BOM
/// </summary>
public class GetEbomVersionHandler : IRequestHandler<GetEbomVersionQuery, EbomVersionResponse>
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IProductRepository _productRepository;

    public GetEbomVersionHandler(
        IBomVersionRepository bomVersionRepository,
        IProductRepository productRepository)
    {
        _bomVersionRepository = bomVersionRepository;
        _productRepository = productRepository;
    }

    public async Task<EbomVersionResponse> Handle(GetEbomVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await _bomVersionRepository.GetByIdAsync(request.BomVersionId, cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException($"BOM version with ID {request.BomVersionId} not found");
        }

        var product = await _productRepository.GetByIdAsync(version.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {version.ProductId} not found");
        }

        // В API поле ItemId — это MDM ItemId (product.ItemId), а не engineering ProductId.
        var dto = new EbomVersionDto(
            version.Id,
            product.ItemId,
            version.VersionCode,
            version.Status.ToString(),
            version.Source.ToString(),
            version.UpdatedAt
        );

        return new EbomVersionResponse(dto);
    }
}