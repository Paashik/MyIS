using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class ItemSequence
{
    public Guid Id { get; private set; }

    public ItemKind ItemKind { get; private set; }

    public string Prefix { get; private set; } = string.Empty;

    public int NextNumber { get; private set; }

    private ItemSequence()
    {
        // For EF Core
    }

    public ItemSequence(ItemKind itemKind, string prefix, int nextNumber)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
        }

        if (nextNumber < 0)
        {
            throw new ArgumentException("NextNumber cannot be negative.", nameof(nextNumber));
        }

        Id = Guid.NewGuid();
        ItemKind = itemKind;
        Prefix = prefix.Trim();
        NextNumber = nextNumber;
    }

    public void IncrementNextNumber()
    {
        NextNumber++;
    }

    public void SetPrefixAndNextNumber(string prefix, int nextNumber)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
        }

        if (nextNumber < 0)
        {
            throw new ArgumentException("NextNumber cannot be negative.", nameof(nextNumber));
        }

        prefix = prefix.Trim().ToUpperInvariant();
        Prefix = prefix;
        NextNumber = nextNumber;
    }

    public void EnsureNextNumberAtLeast(int nextNumber)
    {
        if (nextNumber < 0)
        {
            throw new ArgumentException("NextNumber cannot be negative.", nameof(nextNumber));
        }

        if (nextNumber > NextNumber)
        {
            NextNumber = nextNumber;
        }
    }
}
