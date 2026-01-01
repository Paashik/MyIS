namespace MyIS.Core.Application.Engineering;

/// <summary>
/// Общие DTO для Engineering модуля
/// </summary>

/// <summary>
/// DTO версии BOM
/// </summary>
public record EbomVersionDto(
    Guid Id,
    Guid ItemId,
    string VersionCode,
    string Status,
    string Source,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// DTO строки BOM
/// </summary>
public record EbomLineDto(
    Guid Id,
    Guid ParentItemId,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string Role,
    decimal Qty,
    string UomCode,
    string? PositionNo,
    string? Notes,
    string LineStatus
);
