using MediatR;

namespace MyIS.Core.Application.Engineering.Queries;

/// <summary>
/// Запрос на получение дерева структуры BOM
/// </summary>
public record GetEbomTreeQuery(
    Guid BomVersionId,
    bool IncludeLeaves = false,
    string? Search = null) : IRequest<EbomTreeResponse>;

/// <summary>
/// Ответ с деревом BOM
/// </summary>
public record EbomTreeResponse(EbomTreeDto Tree);

/// <summary>
/// DTO дерева BOM
/// </summary>
public record EbomTreeDto(
    Guid RootItemId,
    IReadOnlyList<EbomTreeNodeDto> Nodes
);

/// <summary>
/// DTO узла дерева BOM
/// </summary>
public record EbomTreeNodeDto(
    Guid ItemId,
    Guid? ParentItemId,
    string Code,
    string Name,
    string ItemType,
    bool HasErrors
);