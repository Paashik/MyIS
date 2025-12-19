using System;

namespace MyIS.Core.WebApi.Dto.Integration;

public sealed class Component2020MdbFileDto
{
    public string Name { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string FullPath { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }
}

public sealed class GetComponent2020MdbFilesResponse
{
    public string DatabasesRoot { get; init; } = string.Empty;

    public Component2020MdbFileDto[] Files { get; init; } = Array.Empty<Component2020MdbFileDto>();
}

