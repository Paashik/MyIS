using System;

namespace MyIS.Core.WebApi.Dto.Integration;

public sealed class Component2020FsEntryDto
{
    public string Name { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public bool IsDirectory { get; init; }

    public long? SizeBytes { get; init; }

    public DateTime? LastWriteTimeUtc { get; init; }
}

public sealed class GetComponent2020FsEntriesResponse
{
    public string DatabasesRoot { get; init; } = string.Empty;

    public string CurrentRelativePath { get; init; } = string.Empty;

    public Component2020FsEntryDto[] Entries { get; init; } = Array.Empty<Component2020FsEntryDto>();
}

