using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class CounterpartyRole
{
    public Guid Id { get; private set; }

    public Guid CounterpartyId { get; private set; }

    public int RoleType { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private CounterpartyRole()
    {
    }

    public CounterpartyRole(Guid counterpartyId, int roleType)
    {
        Id = Guid.NewGuid();
        CounterpartyId = counterpartyId;
        RoleType = roleType;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
