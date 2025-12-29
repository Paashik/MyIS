using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestOrgUnitLookupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Code { get; init; }
    public Guid? ParentId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}
