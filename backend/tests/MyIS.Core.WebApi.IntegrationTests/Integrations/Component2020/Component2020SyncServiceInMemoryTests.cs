using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Integration.Component2020.Repositories;
using MyIS.Core.Infrastructure.Integration.Component2020.Services;
using MyIS.Core.WebApi.IntegrationTests;
using Xunit;

namespace MyIS.Core.WebApi.IntegrationTests.Integrations.Component2020;

public sealed class Component2020SyncServiceInMemoryTests
{
    private sealed class FakeSnapshotReader : IComponent2020SnapshotReader
    {
        public Task<IEnumerable<Component2020Item>> ReadItemsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Item>());

        public Task<IEnumerable<Component2020ItemGroup>> ReadItemGroupsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020ItemGroup>());

        public Task<IEnumerable<Component2020Unit>> ReadUnitsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Unit>());

        public Task<IEnumerable<Component2020Attribute>> ReadAttributesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Attribute>());
    }

    private sealed class FakeDeltaReader : IComponent2020DeltaReader
    {
        private readonly IReadOnlyList<Component2020Unit> _units;
        private readonly IReadOnlyList<Component2020Supplier> _suppliers;

        public FakeDeltaReader(IReadOnlyList<Component2020Unit> units, IReadOnlyList<Component2020Supplier> suppliers)
        {
            _units = units;
            _suppliers = suppliers;
        }

        public Task<IEnumerable<Component2020Unit>> ReadUnitsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Component2020Unit>>(_units);

        public Task<IEnumerable<Component2020Supplier>> ReadSuppliersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Component2020Supplier>>(_suppliers);

        public Task<IEnumerable<Component2020Item>> ReadItemsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Item>());

        public Task<IEnumerable<Component2020Product>> ReadProductsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Product>());

        public Task<IEnumerable<Component2020Manufacturer>> ReadManufacturersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Manufacturer>());

        public Task<IEnumerable<Component2020BodyType>> ReadBodyTypesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020BodyType>());

        public Task<IEnumerable<Component2020Currency>> ReadCurrenciesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Currency>());

        public Task<IEnumerable<Component2020TechnicalParameter>> ReadTechnicalParametersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020TechnicalParameter>());

        public Task<IEnumerable<Component2020ParameterSet>> ReadParameterSetsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020ParameterSet>());

        public Task<IEnumerable<Component2020Symbol>> ReadSymbolsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Symbol>());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"MyIS_Component2020Sync_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RunSyncAsync_Suppliers_InsertsAndUpdatesCursor()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var deltaReader = new FakeDeltaReader(Array.Empty<Component2020Unit>(), new[]
        {
            new Component2020Supplier { Id = 1, Name = "S1", FullName = "Supplier 1", ProviderType = 1 },
            new Component2020Supplier { Id = 2, Name = "S2", ProviderType = 1 }
        });

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Counterparties,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");
        response.ProcessedCount.Should().Be(2);

        var counterparties = await db.Counterparties
            .OrderBy(s => s.Name)
            .ToListAsync();
        counterparties.Should().HaveCount(2);
        counterparties[0].Name.Should().Be("S1");

        var roles = await db.CounterpartyRoles.ToListAsync();
        roles.Should().HaveCount(2);
        roles.All(r => r.RoleType == MyIS.Core.Domain.Mdm.Entities.CounterpartyRoleTypes.Supplier).Should().BeTrue();

        var links = await db.CounterpartyExternalLinks
            .OrderBy(l => l.ExternalId)
            .ToListAsync();
        links.Should().HaveCount(2);
        links[0].ExternalSystem.Should().Be("Component2020");
        links[0].ExternalEntity.Should().Be("Providers");
        links[0].ExternalId.Should().Be("1");

        var cursor = await db.Component2020SyncCursors.SingleAsync(c => c.ConnectionId == connectionId && c.SourceEntity == "Providers");
        cursor.LastProcessedKey.Should().Be("2");
    }

    [Fact]
    public async Task RunSyncAsync_Suppliers_UpdatesExistingSupplier()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var existingCounterparty = new MyIS.Core.Domain.Mdm.Entities.Counterparty(null, "Old", null, null, null, null, null, null, null, null, null);
        db.Counterparties.Add(existingCounterparty);
        db.CounterpartyRoles.Add(new MyIS.Core.Domain.Mdm.Entities.CounterpartyRole(existingCounterparty.Id, MyIS.Core.Domain.Mdm.Entities.CounterpartyRoleTypes.Supplier));
        db.CounterpartyExternalLinks.Add(new MyIS.Core.Domain.Mdm.Entities.CounterpartyExternalLink(existingCounterparty.Id, "Component2020", "Providers", "1", 1, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(Array.Empty<Component2020Unit>(), new[]
        {
            new Component2020Supplier { Id = 1, Name = "New", FullName = "Updated", ProviderType = 1 }
        });

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Counterparties,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");
        response.ProcessedCount.Should().Be(1);

        var counterparty = await db.Counterparties.SingleAsync(c => c.Name == "New");
        counterparty.FullName.Should().Be("Updated");
    }

    [Fact]
    public async Task RunSyncAsync_Suppliers_CapturesPerEntityErrors_AndMarksRunFailed()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var deltaReader = new FakeDeltaReader(Array.Empty<Component2020Unit>(), new[]
        {
            new Component2020Supplier { Id = 1, Name = null!, ProviderType = 1 }
        });

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Counterparties,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Failed");
        response.ProcessedCount.Should().Be(0);

        var run = await db.Component2020SyncRuns.SingleAsync();
        run.Status.Should().Be("Failed");
        run.ErrorCount.Should().Be(1);

        var error = await db.Component2020SyncErrors.SingleAsync();
        error.EntityType.Should().Be("Counterparty");
        error.ExternalKey.Should().Be("1");
    }

    [Fact]
    public async Task RunSyncAsync_Units_UsesNumericCodeAsCode_AndStoresSymbol()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "Штуки", Symbol = "Шт.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Units,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");
        response.ProcessedCount.Should().Be(1);

        var unit = await db.UnitOfMeasures.SingleAsync();
        unit.Code.Should().Be("796");
        unit.Name.Should().Be("Штуки");
        unit.Symbol.Should().Be("Шт.");
        unit.ExternalSystem.Should().Be("Component2020");
        unit.ExternalId.Should().Be("1");

        var cursor = await db.Component2020SyncCursors.SingleAsync(c => c.ConnectionId == connectionId && c.SourceEntity == "Units");
        cursor.LastProcessedKey.Should().Be("1");
    }

    [Fact]
    public async Task RunSyncAsync_Units_UpgradesLegacyIdBasedCode_ToNumericCode()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Legacy record without external linkage
        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("1", "Штуки", "Шт."));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "Штуки", Symbol = "Шт.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Units,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");
        response.ProcessedCount.Should().Be(1);

        var unit = await db.UnitOfMeasures.SingleAsync();
        unit.Code.Should().Be("796");
        unit.Name.Should().Be("Штуки");
        unit.Symbol.Should().Be("Шт.");
        unit.ExternalSystem.Should().Be("Component2020");
        unit.ExternalId.Should().Be("1");
    }

    [Fact]
    public async Task RunSyncAsync_Units_AllowsEmptyCode_AndMatchesByName()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure(null, "Метр", "м"));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 3, Name = "Метр", Symbol = "м", Code = null }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Units,
            DryRun = false,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");
        response.ProcessedCount.Should().Be(1);

        var unit = await db.UnitOfMeasures.SingleAsync();
        unit.Name.Should().Be("Метр");
        unit.Symbol.Should().Be("м");
        unit.Code.Should().BeNull();
    }

    [Fact]
    public async Task RunSyncAsync_Units_Overwrite_DeletesMissingExternalUnits()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var keep = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("796", "Штуки", "шт.");
        keep.SetExternalReference("Component2020", "1", DateTimeOffset.UtcNow);
        db.UnitOfMeasures.Add(keep);

        var toDelete = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("999", "Лишняя", "лш");
        toDelete.SetExternalReference("Component2020", "2", DateTimeOffset.UtcNow);
        db.UnitOfMeasures.Add(toDelete);

        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure(null, "Локальная", "лок"));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "Штуки", Symbol = "шт.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Units,
            DryRun = false,
            SyncMode = Component2020SyncMode.Overwrite,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Success");

        var units = await db.UnitOfMeasures.OrderBy(u => u.Name).ToListAsync();
        units.Should().HaveCount(2);
        units.Select(u => u.Name).Should().Contain(new[] { "Штуки", "Локальная" });
    }

    [Fact]
    public async Task RunSyncAsync_Units_Overwrite_SkipsDeletion_WhenReferenced()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var keep = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("796", "Штуки", "шт.");
        keep.SetExternalReference("Component2020", "1", DateTimeOffset.UtcNow);
        db.UnitOfMeasures.Add(keep);

        var referenced = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("999", "Нельзя удалить", "н/у");
        referenced.SetExternalReference("Component2020", "2", DateTimeOffset.UtcNow);
        db.UnitOfMeasures.Add(referenced);
        await db.SaveChangesAsync();

        db.Items.Add(new MyIS.Core.Domain.Mdm.Entities.Item("X1", "Деталь", MyIS.Core.Domain.Mdm.Entities.ItemKind.Component, referenced.Id));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "Штуки", Symbol = "шт.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance);

        var response = await service.RunSyncAsync(new RunComponent2020SyncCommand
        {
            ConnectionId = connectionId,
            Scope = Component2020SyncScope.Units,
            DryRun = false,
            SyncMode = Component2020SyncMode.Overwrite,
            StartedByUserId = TestAuthHandler.TestUserId
        }, CancellationToken.None);

        response.Status.Should().Be("Partial");

        var unitNames = await db.UnitOfMeasures.Select(u => u.Name).ToListAsync();
        unitNames.Should().Contain("Нельзя удалить");

        (await db.Component2020SyncErrors.AnyAsync(e => e.EntityType == "UnitOfMeasure"))
            .Should()
            .BeTrue();
    }
}
