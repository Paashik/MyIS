using System;

namespace MyIS.Core.Application.Integration.Component2020.Commands;

public class RunComponent2020SyncCommand
{
    public Guid ConnectionId { get; set; }
    public Component2020SyncScope Scope { get; set; }
    public bool DryRun { get; set; }
    public Component2020SyncMode SyncMode { get; set; } = Component2020SyncMode.Delta;
    public Guid? StartedByUserId { get; set; }
}

public enum Component2020SyncMode
{
    Delta,
    SnapshotUpsert,
    Overwrite
}

public enum Component2020SyncScope
{
    Units,
    Counterparties,
    Suppliers = Counterparties,
    ItemGroups,
    Items,
    Products,
    Manufacturers,
    BodyTypes,
    Currencies,
    TechnicalParameters,
    ParameterSets,
    Symbols,
    Employees,
    Users,
    CustomerOrders,
    Statuses,
    All
}

public class RunComponent2020SyncResponse
{
    public Guid RunId { get; set; }
    public string Status { get; set; } // e.g., "Started", "Completed", "Failed"
    public string? ErrorMessage { get; set; }
    public int ProcessedCount { get; set; }
}
