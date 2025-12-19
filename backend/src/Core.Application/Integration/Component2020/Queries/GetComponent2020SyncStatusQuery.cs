using System;

namespace MyIS.Core.Application.Integration.Component2020.Queries;

public class GetComponent2020SyncStatusQuery
{
}

public class GetComponent2020SyncStatusResponse
{
    public bool IsConnected { get; set; }
    public string? ConnectionError { get; set; }
    public bool IsSchedulerActive { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastSyncStatus { get; set; }
}