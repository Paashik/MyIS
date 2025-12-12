namespace MyIS.Core.WebApi.Contracts.Admin.Requests;

public sealed class AdminRequestTypeCreateRequest
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Direction { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

