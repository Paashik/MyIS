using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Engineering.Entities;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик запроса на получение дерева BOM
/// </summary>
public class GetEbomTreeHandler : IRequestHandler<GetEbomTreeQuery, EbomTreeResponse>
{
    private const int MaxNodes = 5000;
    private const int MaxDepth = 64;

    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IProductRepository _productRepository;
    private readonly IItemRepository _itemRepository;

    public GetEbomTreeHandler(
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

    public async Task<EbomTreeResponse> Handle(GetEbomTreeQuery request, CancellationToken cancellationToken)
    {
        var version = await _bomVersionRepository.GetByIdAsync(request.BomVersionId, cancellationToken);
        if (version == null)
        {
            throw new KeyNotFoundException($"BOM version with ID {request.BomVersionId} not found");
        }

        // eBOM хранит ссылки на mdm.items (см. FK в конфигурациях Engineering),
        // поэтому дерево строим по ItemId (MDM), а не по ProductId (Engineering).
        var rootProduct = await _productRepository.GetByIdAsync(version.ProductId, cancellationToken);
        if (rootProduct == null)
        {
            throw new KeyNotFoundException($"Product with ID {version.ProductId} not found");
        }

        var rootItemId = rootProduct.ItemId;

        // Загружаем все строки версии один раз (без N+1)
        var lines = await _bomLineRepository.GetByBomVersionIdAsync(request.BomVersionId, cancellationToken);

        // adjacency: parent -> children
        var childrenByParent = lines
            .GroupBy(x => x.ParentItemId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Guid>)g.Select(x => x.ItemId).Distinct().ToList());

        // Для hasErrors (пока без валидации — только по статусам строк)
        var hasDirectErrorsByParent = lines
            .Where(x => x.Status is LineStatus.Error or LineStatus.Warning)
            .GroupBy(x => x.ParentItemId)
            .ToDictionary(g => g.Key, _ => true);

        // Батч-загрузка MDM items
        var allItemIds = new HashSet<Guid> { rootItemId };
        foreach (var line in lines)
        {
            allItemIds.Add(line.ParentItemId);
            allItemIds.Add(line.ItemId);
        }

        var itemsById = await _itemRepository.GetByIdsAsync(allItemIds, cancellationToken);

        var search = (request.Search ?? string.Empty).Trim();
        var hasSearch = search.Length > 0;

        HashSet<Guid>? includeSet = null;
        if (hasSearch)
        {
            includeSet = BuildSearchIncludeSet(rootItemId, search, lines, itemsById);
        }

        var nodes = new List<EbomTreeNodeDto>(capacity: Math.Min(allItemIds.Count, MaxNodes));
        var visited = new HashSet<Guid>();

        BuildTree(
            rootItemId: rootItemId,
            parentItemId: null,
            currentItemId: rootItemId,
            requestIncludeLeaves: request.IncludeLeaves,
            hasSearch: hasSearch,
            searchIncludeSet: includeSet,
            depth: 0,
            nodes: nodes,
            visited: visited,
            childrenByParent: childrenByParent,
            hasDirectErrorsByParent: hasDirectErrorsByParent,
            itemsById: itemsById);

        var tree = new EbomTreeDto(rootItemId, nodes);
        return new EbomTreeResponse(tree);
    }

    private static HashSet<Guid> BuildSearchIncludeSet(
        Guid rootItemId,
        string search,
        IReadOnlyList<BomLine> lines,
        IReadOnlyDictionary<Guid, Item> itemsById)
    {
        var include = new HashSet<Guid> { rootItemId };

        // В текущей UI-модели itemId уникален в дереве (key=itemId), поэтому берём “первого родителя”.
        // Для MVP достаточно; позже лучше перейти на key по BomLineId/Path.
        var parentByChild = new Dictionary<Guid, Guid>();
        foreach (var line in lines)
        {
            parentByChild.TryAdd(line.ItemId, line.ParentItemId);
        }

        foreach (var (id, item) in itemsById)
        {
            if (Matches(item, search))
            {
                include.Add(id);

                var cursor = id;
                var guard = 0;
                while (guard++ < MaxDepth && parentByChild.TryGetValue(cursor, out var parent))
                {
                    include.Add(parent);
                    if (parent == rootItemId) break;
                    cursor = parent;
                }
            }
        }

        return include;
    }

    private static bool Matches(Item item, string search)
    {
        if (item is null) return false;

        return (item.Code != null && item.Code.Contains(search, StringComparison.OrdinalIgnoreCase))
               || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool BuildTree(
        Guid rootItemId,
        Guid? parentItemId,
        Guid currentItemId,
        bool requestIncludeLeaves,
        bool hasSearch,
        HashSet<Guid>? searchIncludeSet,
        int depth,
        List<EbomTreeNodeDto> nodes,
        HashSet<Guid> visited,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> childrenByParent,
        IReadOnlyDictionary<Guid, bool> hasDirectErrorsByParent,
        IReadOnlyDictionary<Guid, Item> itemsById)
    {
        if (depth > MaxDepth) return false;
        if (nodes.Count >= MaxNodes) return false;

        // Защита от циклов/дубликатов. (Пока не поддерживаем “алмазы” — один item в разных ветках.)
        if (!visited.Add(currentItemId))
        {
            return false;
        }

        if (hasSearch && searchIncludeSet != null && !searchIncludeSet.Contains(currentItemId) && currentItemId != rootItemId)
        {
            return false;
        }

        var hasAnyChildren = childrenByParent.TryGetValue(currentItemId, out var children) && children.Count > 0;

        // По умолчанию (includeLeaves=false) дерево “сборочное”: показываем только узлы с детьми, но корень — всегда.
        // При поиске leaf-узлы показываем, иначе поиск по листьям будет бесполезен.
        if (!hasSearch && !requestIncludeLeaves && currentItemId != rootItemId && !hasAnyChildren)
        {
            return false;
        }

        itemsById.TryGetValue(currentItemId, out var item);

        var code = item?.Code ?? "N/A";
        var name = item?.Name ?? $"[Item {currentItemId}]";
        var type = item != null ? MapItemKindToTreeItemType(item.ItemKind) : "Component";

        var directErrors = hasDirectErrorsByParent.ContainsKey(currentItemId);

        var childErrors = false;
        if (hasAnyChildren)
        {
            foreach (var childId in children!)
            {
                if (nodes.Count >= MaxNodes) break;

                // Если включён поиск — обходим только те ветки, где есть элементы из includeSet
                if (hasSearch && searchIncludeSet != null && !searchIncludeSet.Contains(childId))
                {
                    continue;
                }

                childErrors |= BuildTree(
                    rootItemId: rootItemId,
                    parentItemId: currentItemId,
                    currentItemId: childId,
                    requestIncludeLeaves: requestIncludeLeaves,
                    hasSearch: hasSearch,
                    searchIncludeSet: searchIncludeSet,
                    depth: depth + 1,
                    nodes: nodes,
                    visited: visited,
                    childrenByParent: childrenByParent,
                    hasDirectErrorsByParent: hasDirectErrorsByParent,
                    itemsById: itemsById);
            }
        }

        nodes.Add(new EbomTreeNodeDto(
            ItemId: currentItemId,
            ParentItemId: parentItemId,
            Code: code,
            Name: name,
            ItemType: type,
            HasErrors: directErrors || childErrors));

        return directErrors || childErrors;
    }

    private static string MapItemKindToTreeItemType(ItemKind kind) => kind switch
    {
        ItemKind.Material => "Material",

        ItemKind.Assembly => "Assembly",
        ItemKind.Product => "Assembly",

        ItemKind.ManufacturedPart => "Part",

        ItemKind.PurchasedComponent => "Component",
        ItemKind.StandardPart => "Component",
        ItemKind.ServiceWork => "Component",
        ItemKind.Tool => "Component",
        ItemKind.Equipment => "Component",

        _ => "Component"
    };
}