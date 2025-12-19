using System;

namespace MyIS.Core.WebApi.Dto.Integration;

public class Component2020StatusResponse
{
    public bool IsConnected { get; set; }
    public string? ConnectionError { get; set; }
    public bool IsSchedulerActive { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastSyncStatus { get; set; }
}