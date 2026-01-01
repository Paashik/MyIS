using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Commands;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Engineering.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик команды создания строки BOM
/// </summary>
public class CreateEbomLineHandler : IRequestHandler<CreateEbomLineCommand, CreateEbomLineResponse>
{
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IItemRepository _itemRepository;

    public CreateEbomLineHandler(
        IBomLineRepository bomLineRepository,
        IItemRepository itemRepository)
    {
        _bomLineRepository = bomLineRepository;
        _itemRepository = itemRepository;
    }

    public async Task<CreateEbomLineResponse> Handle(CreateEbomLineCommand request, CancellationToken cancellationToken)
    {
        // engineering.bom_lines.item_id ссылается на mdm.items.id
        var item = await _itemRepository.FindByIdAsync(request.ItemId);
        if (item == null)
        {
            throw new KeyNotFoundException($"Item with ID {request.ItemId} not found");
        }

        // Парсим роль
        if (!Enum.TryParse<BomRole>(request.Role, out var role))
        {
            throw new ArgumentException($"Invalid role: {request.Role}");
        }

        // Создаем строку BOM
        var bomLine = new BomLine(
            request.BomVersionId,
            request.ParentItemId,
            request.ItemId,
            role,
            request.Qty,
            "шт" // TODO: перейти на UnitOfMeasureId и подтягивать из MDM
        );

        // Обновляем дополнительные поля
        bomLine.Update(positionNo: request.PositionNo, notes: request.Notes);

        await _bomLineRepository.AddAsync(bomLine, cancellationToken);

        // Создаем DTO для ответа
        var dto = new EbomLineDto(
            bomLine.Id,
            bomLine.ParentItemId,
            bomLine.ItemId,
            item.Code ?? "N/A",
            item.Name,
            bomLine.Role.ToString(),
            bomLine.Quantity,
            bomLine.UnitOfMeasure ?? "шт",
            bomLine.PositionNo,
            bomLine.Notes,
            bomLine.Status.ToString()
        );

        return new CreateEbomLineResponse(dto);
    }
}