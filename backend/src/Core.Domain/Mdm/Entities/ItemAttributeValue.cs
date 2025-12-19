using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class ItemAttributeValue
{
    public Guid ItemId { get; private set; }

    public Guid AttributeId { get; private set; }

    public string Value { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Item? Item { get; private set; }

    public ItemAttribute? Attribute { get; private set; }

    private ItemAttributeValue()
    {
        // For EF Core
    }

    public ItemAttributeValue(Guid itemId, Guid attributeId, string value)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("ItemId cannot be empty.", nameof(itemId));
        }

        if (attributeId == Guid.Empty)
        {
            throw new ArgumentException("AttributeId cannot be empty.", nameof(attributeId));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        ItemId = itemId;
        AttributeId = attributeId;
        Value = value.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Value = value.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}