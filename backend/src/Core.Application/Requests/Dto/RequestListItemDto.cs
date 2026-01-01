using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestListItemDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public Guid RequestTypeId { get; init; }

    public string RequestTypeName { get; init; } = null!;

    public Guid RequestStatusId { get; init; }

    public string RequestStatusCode { get; init; } = null!;

    public string RequestStatusName { get; init; } = null!;

    public Guid InitiatorId { get; init; }

    public string? ManagerFullName { get; init; }

    public string? TargetEntityName { get; init; }

    public string? RelatedEntityName { get; init; }

    public string? Description { get; init; }

    public string? BasisType { get; init; }

    public Guid? BasisRequestId { get; init; }

    public Guid? BasisCustomerOrderId { get; init; }

    public string? BasisDescription { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? DueDate { get; init; }
}



