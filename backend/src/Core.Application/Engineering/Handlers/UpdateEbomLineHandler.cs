using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Commands;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик команды обновления строки BOM
/// </summary>
public class UpdateEbomLineHandler : IRequestHandler<UpdateEbomLineCommand, UpdateEbomLineResponse>
{
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IItemRepository _itemRepository;

    public UpdateEbomLineHandler(
        IBomLineRepository bomLineRepository,
        IItemRepository itemRepository)
    {
        _bomLineRepository = bomLineRepository;
        _itemRepository = itemRepository;
    }

    public async Task<UpdateEbomLineResponse> Handle(UpdateEbomLineCommand request, CancellationToken cancellationToken)
    {
        var bomLine = await _bomLineRepository.GetByIdAsync(request.LineId, cancellationToken);
        if (bomLine == null)
        {
            throw new KeyNotFoundException($"BOM line with ID {request.LineId} not found");
        }

        // Парсим роль если передана
        BomRole? role = null;
        if (request.Role != null && Enum.TryParse<BomRole>(request.Role, out var parsedRole))
        {
            role = parsedRole;
        }

        if (request.ItemId.HasValue)
        {
            // engineering.bom_lines.item_id ссылается на mdm.items.id
            var itemExists = await _itemRepository.FindByIdAsync(request.ItemId.Value) != null;
            if (!itemExists)
            {
                throw new KeyNotFoundException($"Item with ID {request.ItemId.Value} not found");
            }
        }

        // Обновляем строку
        bomLine.Update(
            role.HasValue ? role.Value : null,
            request.Qty,
            request.PositionNo,
            request.Notes,
            request.ItemId
        );

        await _bomLineRepository.UpdateAsync(bomLine, cancellationToken);

        // Получаем item для DTO (но не валимся, если item отсутствует — возвращаем заглушки)
        var item = await _itemRepository.FindByIdAsync(bomLine.ItemId);

        var itemCode = item?.Code ?? "N/A";
        var itemName = item?.Name ?? $"[Item {bomLine.ItemId}]";

        var dto = new EbomLineDto(
            bomLine.Id,
            bomLine.ParentItemId,
            bomLine.ItemId,
            itemCode,
            itemName,
            bomLine.Role.ToString(),
            bomLine.Quantity,
            bomLine.UnitOfMeasure ?? "шт",
            bomLine.PositionNo,
            bomLine.Notes,
            bomLine.Status.ToString()
        );

        return new UpdateEbomLineResponse(dto);
    }
}