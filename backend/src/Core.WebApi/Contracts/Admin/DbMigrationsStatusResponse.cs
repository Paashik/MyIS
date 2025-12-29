using System.Collections.Generic;

namespace MyIS.Core.WebApi.Contracts.Admin;

public sealed class DbMigrationsStatusResponse
{
    public bool CanConnect { get; init; }

    public string? LastError { get; init; }

    public IEnumerable<string>? AllMigrations { get; init; }

    public IEnumerable<string>? AppliedMigrations { get; init; }

    public IEnumerable<string>? PendingMigrations { get; init; }
}
