namespace MyIS.Core.WebApi.Contracts.Admin.Requests;

public sealed class AdminRequestStatusCreateRequest
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsFinal { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

