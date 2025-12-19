using System;
using MyIS.Core.Application.Integration.Component2020.Commands;

namespace MyIS.Core.WebApi.Dto.Integration;

public class Component2020SyncRunDto
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = null!;
    public Component2020SyncScope Scope { get; set; }
    public bool DryRun { get; set; }
    public int ProcessedCount { get; set; }
    public string? ErrorMessage { get; set; }
}