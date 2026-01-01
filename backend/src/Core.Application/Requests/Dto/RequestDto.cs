using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    /// <summary>
    /// Текстовое тело заявки (на текущей итерации совпадает с Description).
    /// </summary>
    public string? BodyText { get; init; }

    public Guid RequestTypeId { get; init; }

    public string RequestTypeName { get; init; } = null!;

    public Guid RequestStatusId { get; init; }

    public string RequestStatusCode { get; init; } = null!;

    public string RequestStatusName { get; init; } = null!;

    public Guid ManagerId { get; init; }

    /// <summary>
    /// Алиас для совместимости: инициатор заявки = ManagerId (на текущей итерации).
    /// </summary>
    public Guid InitiatorId { get; init; }

    public string? ManagerFullName { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityName { get; init; }

    /// <summary>
    /// Ссылка на внешний объект (например, Компонент-2020).
    /// </summary>
    public string? ExternalReferenceId { get; init; }

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



