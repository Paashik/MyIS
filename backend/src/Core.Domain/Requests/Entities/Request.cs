using System;
using System.Collections.Generic;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class Request
{
    // Primary key
    public RequestId Id { get; private set; }

    // Basic info
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }

    // Type and status
    public RequestTypeId RequestTypeId { get; private set; }
    public RequestStatusId RequestStatusId { get; private set; }

    // User and relations
    public Guid InitiatorId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? ExternalReferenceId { get; private set; }

    // Audit / timeline
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    // Navigation properties
    public RequestType? Type { get; private set; }
    public RequestStatus? Status { get; private set; }

    public ICollection<RequestHistory> History { get; private set; } = new List<RequestHistory>();
    public ICollection<RequestComment> Comments { get; private set; } = new List<RequestComment>();
    public ICollection<RequestAttachment> Attachments { get; private set; } = new List<RequestAttachment>();

    private Request()
    {
        // For EF Core
    }

    private Request(
        RequestId id,
        string title,
        string? description,
        RequestTypeId typeId,
        RequestStatusId statusId,
        Guid initiatorId,
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? externalReferenceId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (initiatorId == Guid.Empty)
        {
            throw new ArgumentException("InitiatorId cannot be empty.", nameof(initiatorId));
        }

        Id = id;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RequestTypeId = typeId;
        RequestStatusId = statusId;
        InitiatorId = initiatorId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        DueDate = dueDate;
        RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim();
        RelatedEntityId = relatedEntityId;
        ExternalReferenceId = string.IsNullOrWhiteSpace(externalReferenceId) ? null : externalReferenceId.Trim();
    }

    public static Request Create(
        RequestType type,
        RequestStatus initialStatus,
        Guid initiatorId,
        string title,
        string? description,
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        string? externalReferenceId = null)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (initialStatus == null) throw new ArgumentNullException(nameof(initialStatus));

        var request = new Request(
            RequestId.New(),
            title,
            description,
            type.Id,
            initialStatus.Id,
            initiatorId,
            createdAt,
            dueDate,
            relatedEntityType,
            relatedEntityId,
            externalReferenceId);

        return request;
    }

    public void UpdateDetails(
        string title,
        string? description,
        DateTimeOffset? dueDate,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? externalReferenceId,
        DateTimeOffset updatedAt,
        bool isCurrentStatusFinal)
    {
        if (isCurrentStatusFinal)
        {
            throw new InvalidOperationException("Cannot update request details when it is in a final status.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DueDate = dueDate;
        RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim();
        RelatedEntityId = relatedEntityId;
        ExternalReferenceId = string.IsNullOrWhiteSpace(externalReferenceId) ? null : externalReferenceId.Trim();
        UpdatedAt = updatedAt;
    }

    public RequestHistory ChangeStatus(
        RequestStatus targetStatus,
        Guid performedBy,
        DateTimeOffset timestamp,
        string action,
        string? comment,
        bool isCurrentStatusFinal)
    {
        if (targetStatus == null) throw new ArgumentNullException(nameof(targetStatus));

        if (performedBy == Guid.Empty)
        {
            throw new ArgumentException("PerformedBy cannot be empty.", nameof(performedBy));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (isCurrentStatusFinal)
        {
            throw new InvalidOperationException("Cannot change status when current status is final.");
        }

        var oldStatusId = RequestStatusId;
        var oldStatusCode = Status?.Code.Value ?? string.Empty;

        RequestStatusId = targetStatus.Id;
        UpdatedAt = timestamp;

        var historyItem = RequestHistory.CreateStatusChange(
            Id,
            action,
            performedBy,
            timestamp,
            oldStatusCode,
            targetStatus.Code.Value,
            comment);

        History.Add(historyItem);

        return historyItem;
    }
}