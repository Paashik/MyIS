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
    public Guid ManagerId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityName { get; private set; }
    public string? TargetEntityType { get; private set; }
    public Guid? TargetEntityId { get; private set; }
    public string? TargetEntityName { get; private set; }
    public string? BasisType { get; private set; }
    public Guid? BasisRequestId { get; private set; }
    public Guid? BasisCustomerOrderId { get; private set; }
    public string? BasisDescription { get; private set; }

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
        Guid managerId,
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? relatedEntityName,
        string? targetEntityType,
        Guid? targetEntityId,
        string? targetEntityName,
        string? basisType,
        Guid? basisRequestId,
        Guid? basisCustomerOrderId,
        string? basisDescription)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (managerId == Guid.Empty)
        {
            throw new ArgumentException("ManagerId cannot be empty.", nameof(managerId));
        }

        Id = id;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RequestTypeId = typeId;
        RequestStatusId = statusId;
        ManagerId = managerId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        DueDate = dueDate;
        RelatedEntityType = string.IsNullOrWhiteSpace(relatedEntityType) ? null : relatedEntityType.Trim();
        RelatedEntityId = relatedEntityId;
        RelatedEntityName = string.IsNullOrWhiteSpace(relatedEntityName) ? null : relatedEntityName.Trim();
        TargetEntityType = string.IsNullOrWhiteSpace(targetEntityType) ? null : targetEntityType.Trim();
        TargetEntityId = targetEntityId;
        TargetEntityName = string.IsNullOrWhiteSpace(targetEntityName) ? null : targetEntityName.Trim();
        ApplyBasis(basisType, basisRequestId, basisCustomerOrderId, basisDescription);
    }

    public static Request Create(
        RequestType type,
        RequestStatus initialStatus,
        Guid managerId,
        string title,
        string? description,
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        string? relatedEntityName = null,
        string? targetEntityType = null,
        Guid? targetEntityId = null,
        string? targetEntityName = null,
        string? basisType = null,
        Guid? basisRequestId = null,
        Guid? basisCustomerOrderId = null,
        string? basisDescription = null)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (initialStatus == null) throw new ArgumentNullException(nameof(initialStatus));

        var request = new Request(
            RequestId.New(),
            title,
            description,
            type.Id,
            initialStatus.Id,
            managerId,
            createdAt,
            dueDate,
            relatedEntityType,
            relatedEntityId,
            relatedEntityName,
            targetEntityType,
            targetEntityId,
            targetEntityName,
            basisType,
            basisRequestId,
            basisCustomerOrderId,
            basisDescription);

        return request;
    }

    public void UpdateDetails(
        string title,
        string? description,
        DateTimeOffset? dueDate,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? relatedEntityName,
        string? targetEntityType,
        Guid? targetEntityId,
        string? targetEntityName,
        string? basisType,
        Guid? basisRequestId,
        Guid? basisCustomerOrderId,
        string? basisDescription,
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
        RelatedEntityName = string.IsNullOrWhiteSpace(relatedEntityName) ? null : relatedEntityName.Trim();
        TargetEntityType = string.IsNullOrWhiteSpace(targetEntityType) ? null : targetEntityType.Trim();
        TargetEntityId = targetEntityId;
        TargetEntityName = string.IsNullOrWhiteSpace(targetEntityName) ? null : targetEntityName.Trim();
        ApplyBasis(basisType, basisRequestId, basisCustomerOrderId, basisDescription);
        UpdatedAt = updatedAt;
    }

    public void ChangeType(RequestType newType)
    {
        if (newType is null) throw new ArgumentNullException(nameof(newType));
        RequestTypeId = newType.Id;
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

        return comment;
    }

    /// <summary>
    /// Р’Р°Р»РёРґР°С†РёСЏ С‚РµР»Р° Р·Р°СЏРІРєРё РґР»СЏ РІС‹С…РѕРґР° РёР· Draft РїРѕ РґРµР№СЃС‚РІРёСЋ Submit.
    /// Р”Р»СЏ SupplyRequest С‚СЂРµР±СѓРµС‚СЃСЏ Р·Р°РїРѕР»РЅРёС‚СЊ Р»РёР±Рѕ Lines, Р»РёР±Рѕ Description.
    /// </summary>
    public void EnsureBodyIsValidForSubmit(RequestTypeId requestTypeId)
    {
        if (requestTypeId == RequestTypeIds.SupplyRequest)
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

    private void ApplyBasis(
        string? basisType,
        Guid? basisRequestId,
        Guid? basisCustomerOrderId,
        string? basisDescription)
    {
        var normalizedType = string.IsNullOrWhiteSpace(basisType) ? null : basisType.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(basisDescription) ? null : basisDescription.Trim();

        if (normalizedType is null)
        {
            BasisType = null;
            BasisRequestId = null;
            BasisCustomerOrderId = null;
            BasisDescription = null;
            return;
        }

        if (!RequestBasisTypes.IsValid(normalizedType))
        {
            throw new ArgumentException($"Unsupported basis type '{normalizedType}'.", nameof(basisType));
        }

        switch (normalizedType)
        {
            case RequestBasisTypes.IncomingRequest:
                if (!basisRequestId.HasValue || basisRequestId == Guid.Empty)
                {
                    throw new ArgumentException("BasisRequestId is required for IncomingRequest.", nameof(basisRequestId));
                }

                BasisRequestId = basisRequestId;
                BasisCustomerOrderId = null;
                BasisDescription = normalizedDescription;
                break;
            case RequestBasisTypes.CustomerOrder:
                if (!basisCustomerOrderId.HasValue || basisCustomerOrderId == Guid.Empty)
                {
                    throw new ArgumentException("BasisCustomerOrderId is required for CustomerOrder.", nameof(basisCustomerOrderId));
                }

                BasisRequestId = null;
                BasisCustomerOrderId = basisCustomerOrderId;
                BasisDescription = normalizedDescription;
                break;
            case RequestBasisTypes.ProductionOrder:
            case RequestBasisTypes.Other:
                BasisRequestId = null;
                BasisCustomerOrderId = null;
                BasisDescription = normalizedDescription;
                break;
        }

        BasisType = normalizedType;
    }
}



