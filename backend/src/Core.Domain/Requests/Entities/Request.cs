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

    public ICollection<RequestLine> Lines { get; private set; } = new List<RequestLine>();

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

    public void ReplaceLines(
        IReadOnlyCollection<RequestLine> newLines,
        DateTimeOffset updatedAt,
        bool isCurrentStatusFinal)
    {
        if (isCurrentStatusFinal)
        {
            throw new InvalidOperationException("Cannot update request lines when it is in a final status.");
        }

        if (newLines is null) throw new ArgumentNullException(nameof(newLines));

        // Validate duplicates
        var seen = new HashSet<int>();
        foreach (var line in newLines)
        {
            if (line is null) throw new ArgumentException("Lines cannot contain null.", nameof(newLines));
            if (line.RequestId != Id)
            {
                throw new InvalidOperationException("All lines must belong to the same request.");
            }

            if (!seen.Add(line.LineNo))
            {
                throw new InvalidOperationException($"Duplicate LineNo '{line.LineNo}'.");
            }
        }

        Lines.Clear();
        foreach (var line in newLines)
        {
            Lines.Add(line);
        }

        UpdatedAt = updatedAt;
    }

    public RequestComment AddComment(
        Guid authorId,
        string text,
        DateTimeOffset createdAt)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", nameof(text));
        }

        var comment = RequestComment.Create(
            requestId: Id,
            authorId: authorId,
            text: text,
            createdAt: createdAt);

        Comments.Add(comment);
        if (createdAt > UpdatedAt)
        {
            UpdatedAt = createdAt;
        }

        return comment;
    }

    /// <summary>
    /// Валидация тела заявки для выхода из Draft по действию Submit.
    /// Для SupplyRequest требуется заполнить либо Lines, либо Description.
    /// </summary>
    public void EnsureBodyIsValidForSubmit(string requestTypeCode)
    {
        if (string.Equals(requestTypeCode, "SupplyRequest", StringComparison.OrdinalIgnoreCase))
        {
            var hasLines = Lines.Count > 0;
            var hasText = !string.IsNullOrWhiteSpace(Description);

            if (!hasLines && !hasText)
            {
                throw new InvalidOperationException("SupplyRequest requires either Lines or Description to be filled before Submit.");
            }
        }
    }

    public RequestHistory ChangeStatus(
        RequestStatus currentStatus,
        RequestStatus targetStatus,
        Guid performedBy,
        DateTimeOffset timestamp,
        string action,
        string? comment)
    {
        if (currentStatus == null) throw new ArgumentNullException(nameof(currentStatus));
        if (targetStatus == null) throw new ArgumentNullException(nameof(targetStatus));

        if (performedBy == Guid.Empty)
        {
            throw new ArgumentException("PerformedBy cannot be empty.", nameof(performedBy));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (currentStatus.IsFinal)
        {
            throw new InvalidOperationException("Cannot change status when current status is final.");
        }

        var oldStatusCode = currentStatus.Code.Value;

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
