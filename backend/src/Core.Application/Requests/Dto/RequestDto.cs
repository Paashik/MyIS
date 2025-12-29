using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    /// <summary>
    /// РўРµРєСЃС‚РѕРІРѕРµ С‚РµР»Рѕ Р·Р°СЏРІРєРё (РЅР° С‚РµРєСѓС‰РµР№ РёС‚РµСЂР°С†РёРё СЃРѕРІРїР°РґР°РµС‚ СЃ Description).
    /// </summary>
    public string? BodyText { get; init; }

    public Guid RequestTypeId { get; init; }

    public string RequestTypeName { get; init; } = null!;

    public Guid RequestStatusId { get; init; }

    public string RequestStatusCode { get; init; } = null!;

    public string RequestStatusName { get; init; } = null!;

    public Guid ManagerId { get; init; }

    public string? ManagerFullName { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityName { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    public string? TargetEntityName { get; init; }

    public string? BasisType { get; init; }

    public Guid? BasisRequestId { get; init; }

    public Guid? BasisCustomerOrderId { get; init; }

    public string? BasisDescription { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public RequestLineDto[] Lines { get; init; } = Array.Empty<RequestLineDto>();
}



