using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Application.Mdm.Abstractions;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса на получение строк BOM
/// </summary>
public class GetEbomLinesHandler : IRequestHandler<GetEbomLinesQuery, EbomLinesResponse>
{
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IItemRepository _itemRepository;

    public GetEbomLinesHandler(
        IBomLineRepository bomLineRepository,
        IItemRepository itemRepository)
    {
        _bomLineRepository = bomLineRepository;
        _itemRepository = itemRepository;
    }

    public async Task<EbomLinesResponse> Handle(GetEbomLinesQuery request, CancellationToken cancellationToken)
    {
        var lines = await _bomLineRepository.GetByParentItemIdAsync(
            request.BomVersionId,
            request.ParentItemId,
            request.OnlyErrors,
            cancellationToken);

        // Батч-загрузка MDM items, чтобы не делать N+1
        var itemIds = lines.Select(x => x.ItemId).Distinct().ToArray();
        var itemsById = await _itemRepository.GetByIdsAsync(itemIds, cancellationToken);

        var dtos = new List<EbomLineDto>(capacity: lines.Count);
        foreach (var line in lines)
        {
            // engineering.bom_lines.item_id ссылается на mdm.items.id
            itemsById.TryGetValue(line.ItemId, out var item);

            // Важно: не теряем строку даже если MDM item отсутствует (legacy/битые данные/частичный импорт)
            var itemCode = item?.Code ?? "N/A";
            var itemName = item?.Name ?? $"[Item {line.ItemId}]";

            dtos.Add(new EbomLineDto(
                line.Id,
                line.ParentItemId,
                line.ItemId,
                itemCode,
                itemName,
                line.Role.ToString(),
                line.Quantity,
                line.UnitOfMeasure ?? "шт",
                line.PositionNo,
                line.Notes,
                line.Status.ToString()
            ));
        }

        return new EbomLinesResponse(dtos);
    }
}