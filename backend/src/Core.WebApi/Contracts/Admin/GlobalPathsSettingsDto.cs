namespace MyIS.Core.WebApi.Contracts.Admin;

public sealed class GlobalPathsSettingsDto
{
    public string ProjectsRoot { get; init; } = string.Empty;

    public string DocumentsRoot { get; init; } = string.Empty;

    public string DatabasesRoot { get; init; } = string.Empty;
}

public sealed class UpdateGlobalPathsSettingsRequest
{
    public string ProjectsRoot { get; init; } = string.Empty;

    public string DocumentsRoot { get; init; } = string.Empty;

    public string DatabasesRoot { get; init; } = string.Empty;

    public bool CreateDirectories { get; init; }
}

public sealed class GlobalPathCheckDto
{
    public bool IsSet { get; init; }

    public bool Exists { get; init; }

    public bool CanWrite { get; init; }

    public string? Error { get; init; }
}

public sealed class GlobalPathsSettingsResponse
{
    public GlobalPathsSettingsDto Settings { get; init; } = new();

    public GlobalPathCheckDto ProjectsRoot { get; init; } = new();

    public GlobalPathCheckDto DocumentsRoot { get; init; } = new();

    public GlobalPathCheckDto DatabasesRoot { get; init; } = new();
}

