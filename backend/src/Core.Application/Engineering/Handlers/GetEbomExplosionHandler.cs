using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Engineering.Entities;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса на "взрыв" спецификации (получение полного состава версии eBOM).
/// Возвращает плоский список строк (каждая строка соответствует одной BomLine) с вычисленным TotalQty,
/// уровнем вложенности и путём (Path) для удобной группировки/отладки.
/// </summary>
public sealed class GetEbomExplosionHandler : IRequestHandler<GetEbomExplosionQuery, EbomExplosionResponse>
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IProductRepository _productRepository;
    private readonly IItemRepository _itemRepository;

    public GetEbomExplosionHandler(
        IBomVersionRepository bomVersionRepository,
        IBomLineRepository bomLineRepository,
        IProductRepository productRepository,
        IItemRepository itemRepository)
    {
        _bomVersionRepository = bomVersionRepository;
        _bomLineRepository = bomLineRepository;
        _productRepository = productRepository;
        _itemRepository = itemRepository;
    }

    public async Task<EbomExplosionResponse> Handle(GetEbomExplosionQuery request, CancellationToken cancellationToken)
    {
        var maxDepth = request.MaxDepth <= 0 ? 1 : Math.Min(request.MaxDepth, 256);
        var maxRows = request.MaxRows <= 0 ? 1 : Math.Min(request.MaxRows, 200_000);

        var version = await _bomVersionRepository.GetByIdAsync(request.BomVersionId, cancellationToken);
        if (version == null)
        {
            throw new KeyNotFoundException($"BOM version with ID {request.BomVersionId} not found");
        }

        var rootProduct = await _productRepository.GetByIdAsync(version.ProductId, cancellationToken);
        if (rootProduct == null)
        {
            throw new KeyNotFoundException($"Product with ID {version.ProductId} not found");
        }

        var rootItemId = rootProduct.ItemId;

        // Загружаем все строки версии один раз (без N+1)
        var lines = await _bomLineRepository.GetByBomVersionIdAsync(request.BomVersionId, cancellationToken);

        // parentItemId -> lines
        var linesByParent = lines
            .GroupBy(x => x.ParentItemId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BomLine>)g.ToList());

        // Батч-загрузка MDM items (и для parent, и для child)
        var allItemIds = new HashSet<Guid> { rootItemId };
        foreach (var line in lines)
        {
            allItemIds.Add(line.ParentItemId);
            allItemIds.Add(line.ItemId);
        }

        var itemsById = await _itemRepository.GetByIdsAsync(allItemIds, cancellationToken);

        var rows = new List<EbomExplosionRowDto>(capacity: Math.Min(lines.Count, maxRows));
        var stack = new HashSet<Guid> { rootItemId };

        Visit(
            parentItemId: rootItemId,
            parentTotalQty: 1m,
            level: 0,
            path: rootItemId.ToString("N"),
            stack: stack,
            maxDepth: maxDepth,
            maxRows: maxRows,
            rows: rows,
            linesByParent: linesByParent,
            itemsById: itemsById);

        return new EbomExplosionResponse(rootItemId, rows);
    }

    private static void Visit(
        Guid parentItemId,
        decimal parentTotalQty,
        int level,
        string path,
        HashSet<Guid> stack,
        int maxDepth,
        int maxRows,
        List<EbomExplosionRowDto> rows,
        IReadOnlyDictionary<Guid, IReadOnlyList<BomLine>> linesByParent,
        IReadOnlyDictionary<Guid, Item> itemsById)
    {
        if (rows.Count >= maxRows) return;
        if (level >= maxDepth) return;

        if (!linesByParent.TryGetValue(parentItemId, out var childLines) || childLines.Count == 0)
        {
            return;
        }

        foreach (var line in childLines)
        {
            if (rows.Count >= maxRows) break;

            // Защита от циклов: не уходим глубже, если встретили itemId на текущем стеке.
            var isCycle = stack.Contains(line.ItemId);

            itemsById.TryGetValue(line.ItemId, out var item);

            var code = item?.Code ?? "N/A";
            var name = item?.Name ?? $"[Item {line.ItemId}]";

            var qty = line.Quantity;
            var totalQty = parentTotalQty * qty;

            var uomCode = line.UnitOfMeasure ?? "шт";
            var role = line.Role.ToString();
            var lineStatus = line.Status.ToString();

            var rowPath = $"{path}>{line.ItemId:N}";

            rows.Add(new EbomExplosionRowDto(
                LineId: line.Id,
                ParentItemId: line.ParentItemId,
                ItemId: line.ItemId,
                ItemCode: code,
                ItemName: name,
                Role: role,
                Qty: qty,
                TotalQty: totalQty,
                UomCode: uomCode,
                PositionNo: line.PositionNo,
                Notes: line.Notes,
                LineStatus: lineStatus,
                Level: level + 1,
                Path: rowPath));

            if (isCycle) continue;

            stack.Add(line.ItemId);
            Visit(
                parentItemId: line.ItemId,
                parentTotalQty: totalQty,
                level: level + 1,
                path: rowPath,
                stack: stack,
                maxDepth: maxDepth,
                maxRows: maxRows,
                rows: rows,
                linesByParent: linesByParent,
                itemsById: itemsById);
            stack.Remove(line.ItemId);

            if (rows.Count >= maxRows) break;
        }
    }
}