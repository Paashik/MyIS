using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public Guid RequestTypeId { get; init; }

    public string RequestTypeCode { get; init; } = null!;

    public string RequestTypeName { get; init; } = null!;

    public Guid RequestStatusId { get; init; }

    public string RequestStatusCode { get; init; } = null!;

    public string RequestStatusName { get; init; } = null!;

    public Guid InitiatorId { get; init; }

    public string? InitiatorFullName { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? ExternalReferenceId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? DueDate { get; init; }
}