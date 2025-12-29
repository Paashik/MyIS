using System;
using MyIS.Core.Application.Integration.Component2020.Commands;

namespace MyIS.Core.Application.Integration.Component2020.Dto;

public sealed class Component2020ImportPreviewRequestDto
{
    public Guid ConnectionId { get; set; }
    public Component2020SyncMode SyncMode { get; set; } = Component2020SyncMode.Delta;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 200;
}

public sealed class Component2020ImportPreviewSummaryDto
{
    public int Total { get; set; }
    public int Products { get; set; }
    public int Components { get; set; }
    public int Create { get; set; }
    public int Update { get; set; }
    public int Merge { get; set; }
    public int Review { get; set; }
}

public sealed class Component2020ImportPreviewItemDto
{
    public string Source { get; set; } = string.Empty;
    public int ExternalId { get; set; }
    public string? ExternalGroupId { get; set; }
    public string? ExternalGroupName { get; set; }
    public string? Code { get; set; }
    public string? PartNumber { get; set; }
    public string? Designation { get; set; }
    public string? DesignationSource { get; set; }
    public string[] DesignationCandidates { get; set; } = Array.Empty<string>();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public string ItemKind { get; set; } = string.Empty;
    public string? ItemGroupId { get; set; }
    public string? ItemGroupName { get; set; }
    public string? RootGroupAbbreviation { get; set; }
    public string Action { get; set; } = string.Empty;
    public string[] Reasons { get; set; } = Array.Empty<string>();
    public string? ExistingItemId { get; set; }
    public string? ExistingItemKind { get; set; }
    public string? ExistingItemGroup { get; set; }
    public string? MatchedItemId { get; set; }
    public string? MatchedItemKind { get; set; }
    public string? MatchedItemGroup { get; set; }
    public bool IsTooling { get; set; }
}

public sealed class Component2020ImportPreviewResponseDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public Component2020ImportPreviewSummaryDto Summary { get; set; } = new();
    public Component2020ImportPreviewItemDto[] Items { get; set; } = Array.Empty<Component2020ImportPreviewItemDto>();
}
