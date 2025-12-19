using System;
using System.Collections.Generic;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Item
{
    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public ItemKind ItemKind { get; private set; }

    public Guid UnitOfMeasureId { get; private set; }

    public bool IsActive { get; private set; }

    public string? ExternalSystem { get; private set; }

    public string? ExternalId { get; private set; }

    public DateTimeOffset? SyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsEskd { get; private set; }

    public bool? IsEskdDocument { get; private set; }

    public string? ManufacturerPartNumber { get; private set; }

    public Guid? ItemGroupId { get; private set; }

    public UnitOfMeasure? UnitOfMeasure { get; private set; }

    public ItemGroup? ItemGroup { get; private set; }

    public ICollection<ItemAttributeValue> AttributeValues { get; private set; } = new List<ItemAttributeValue>();

    private Item()
    {
        // For EF Core
    }

    public Item(string code, string name, ItemKind itemKind, Guid unitOfMeasureId, Guid? itemGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        if (unitOfMeasureId == Guid.Empty)
        {
            throw new ArgumentException("UnitOfMeasureId cannot be empty.", nameof(unitOfMeasureId));
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        Name = name.Trim();
        ItemKind = itemKind;
        UnitOfMeasureId = unitOfMeasureId;
        ItemGroupId = itemGroupId;
        IsActive = true;
        IsEskd = false;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, Guid unitOfMeasureId, Guid? itemGroupId, bool isEskd, bool? isEskdDocument, string? manufacturerPartNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        if (unitOfMeasureId == Guid.Empty)
        {
            throw new ArgumentException("UnitOfMeasureId cannot be empty.", nameof(unitOfMeasureId));
        }

        Name = name.Trim();
        UnitOfMeasureId = unitOfMeasureId;
        ItemGroupId = itemGroupId;
        IsEskd = isEskd;
        IsEskdDocument = isEskdDocument;
        ManufacturerPartNumber = string.IsNullOrWhiteSpace(manufacturerPartNumber) ? null : manufacturerPartNumber.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetExternalReference(string externalSystem, string externalId, DateTimeOffset syncedAt)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem cannot be null or empty.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId cannot be null or empty.", nameof(externalId));
        }

        ExternalSystem = externalSystem.Trim();
        ExternalId = externalId.Trim();
        SyncedAt = syncedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddAttributeValue(ItemAttributeValue value)
    {
        if (value.ItemId != Id)
        {
            throw new ArgumentException("Attribute value does not belong to this item.", nameof(value));
        }

        AttributeValues.Add(value);
    }

    public void RemoveAttributeValue(Guid attributeId)
    {
        var value = AttributeValues.FirstOrDefault(v => v.AttributeId == attributeId);
        if (value != null)
        {
            AttributeValues.Remove(value);
        }
    }
}