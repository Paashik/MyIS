using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyIS.Core.Application.Auth;
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
        public Task<IEnumerable<Component2020Item>> ReadItemsAsync(CancellationToken cancellationToken, Guid? connectionId = null) =>
            Task.FromResult(Enumerable.Empty<Component2020Item>());

        public Task<IEnumerable<Component2020ItemGroup>> ReadItemGroupsAsync(CancellationToken cancellationToken, Guid? connectionId = null) =>
            Task.FromResult(Enumerable.Empty<Component2020ItemGroup>());

        public Task<IEnumerable<Component2020Unit>> ReadUnitsAsync(CancellationToken cancellationToken, Guid? connectionId = null) =>
            Task.FromResult(Enumerable.Empty<Component2020Unit>());

        public Task<IEnumerable<Component2020Attribute>> ReadAttributesAsync(CancellationToken cancellationToken, Guid? connectionId = null) =>
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

        public Task<IEnumerable<Component2020Person>> ReadPersonsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Person>());

        public Task<IEnumerable<Component2020User>> ReadUsersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020User>());

        public Task<IEnumerable<Component2020Role>> ReadRolesAsync(Guid connectionId, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Role>());

        public Task<IEnumerable<Component2020CustomerOrder>> ReadCustomerOrdersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020CustomerOrder>());

        public Task<IEnumerable<Component2020Status>> ReadStatusesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Component2020Status>());
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hash:{password}";
        public bool VerifyHashedPassword(string hash, string password) => hash == $"hash:{password}";
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"MyIS_Component2020Sync_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static readonly IPasswordHasher PasswordHasher = new FakePasswordHasher();

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
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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

        var links = await db.ExternalEntityLinks
            .Where(l => l.EntityType == nameof(MyIS.Core.Domain.Mdm.Entities.Counterparty))
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

        var existingCounterparty = new MyIS.Core.Domain.Mdm.Entities.Counterparty(
            name: "Old",
            fullName: null,
            inn: null,
            kpp: null,
            email: null,
            phone: null,
            city: null,
            address: null,
            site: null,
            siteLogin: null,
            sitePassword: null,
            note: null);
        db.Counterparties.Add(existingCounterparty);
        db.CounterpartyRoles.Add(new MyIS.Core.Domain.Mdm.Entities.CounterpartyRole(existingCounterparty.Id, MyIS.Core.Domain.Mdm.Entities.CounterpartyRoleTypes.Supplier));
        db.ExternalEntityLinks.Add(new MyIS.Core.Domain.Mdm.Entities.ExternalEntityLink(nameof(MyIS.Core.Domain.Mdm.Entities.Counterparty), existingCounterparty.Id, "Component2020", "Providers", "1", 1, DateTimeOffset.UtcNow));
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
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
            new Component2020Unit { Id = 1, Name = "РЁС‚СѓРєРё", Symbol = "РЁС‚.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
        unit.Name.Should().Be("РЁС‚СѓРєРё");
        unit.Symbol.Should().Be("РЁС‚.");
        var link = await db.ExternalEntityLinks.SingleAsync(l => l.EntityType == nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure) && l.EntityId == unit.Id);
        link.ExternalSystem.Should().Be("Component2020");
        link.ExternalEntity.Should().Be("Unit");
        link.ExternalId.Should().Be("1");

        var cursor = await db.Component2020SyncCursors.SingleAsync(c => c.ConnectionId == connectionId && c.SourceEntity == "Units");
        cursor.LastProcessedKey.Should().Be("1");
    }

    [Fact]
    public async Task RunSyncAsync_Units_UpgradesLegacyIdBasedCode_ToNumericCode()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Legacy record without external linkage
        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("1", "РЁС‚СѓРєРё", "РЁС‚."));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "РЁС‚СѓРєРё", Symbol = "РЁС‚.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
        unit.Name.Should().Be("РЁС‚СѓРєРё");
        unit.Symbol.Should().Be("РЁС‚.");
        var link = await db.ExternalEntityLinks.SingleAsync(l => l.EntityType == nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure) && l.EntityId == unit.Id);
        link.ExternalSystem.Should().Be("Component2020");
        link.ExternalEntity.Should().Be("Unit");
        link.ExternalId.Should().Be("1");
    }

    [Fact]
    public async Task RunSyncAsync_Units_AllowsEmptyCode_AndMatchesByName()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure(null, "РњРµС‚СЂ", "Рј"));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 3, Name = "РњРµС‚СЂ", Symbol = "Рј", Code = null }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
        unit.Name.Should().Be("РњРµС‚СЂ");
        unit.Symbol.Should().Be("Рј");
        unit.Code.Should().BeNull();
    }

    [Fact]
    public async Task RunSyncAsync_Units_Overwrite_DeletesMissingExternalUnits()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var keep = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("796", "РЁС‚СѓРєРё", "С€С‚.");
        db.UnitOfMeasures.Add(keep);

        var toDelete = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("999", "Р›РёС€РЅСЏСЏ", "Р»С€");
        db.UnitOfMeasures.Add(toDelete);

        db.UnitOfMeasures.Add(new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure(null, "Р›РѕРєР°Р»СЊРЅР°СЏ", "Р»РѕРє"));
        db.ExternalEntityLinks.Add(new MyIS.Core.Domain.Mdm.Entities.ExternalEntityLink(nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure), keep.Id, "Component2020", "Unit", "1", null, DateTimeOffset.UtcNow));
        db.ExternalEntityLinks.Add(new MyIS.Core.Domain.Mdm.Entities.ExternalEntityLink(nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure), toDelete.Id, "Component2020", "Unit", "2", null, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "РЁС‚СѓРєРё", Symbol = "С€С‚.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
        units.Select(u => u.Name).Should().Contain(new[] { "РЁС‚СѓРєРё", "Р›РѕРєР°Р»СЊРЅР°СЏ" });
    }

    [Fact]
    public async Task RunSyncAsync_Units_Overwrite_SkipsDeletion_WhenReferenced()
    {
        await using var db = CreateDbContext();

        var connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var keep = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("796", "РЁС‚СѓРєРё", "С€С‚.");
        db.UnitOfMeasures.Add(keep);

        var referenced = new MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure("999", "РќРµР»СЊР·СЏ СѓРґР°Р»РёС‚СЊ", "РЅ/Сѓ");
        db.UnitOfMeasures.Add(referenced);
        db.ExternalEntityLinks.Add(new MyIS.Core.Domain.Mdm.Entities.ExternalEntityLink(nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure), keep.Id, "Component2020", "Unit", "1", null, DateTimeOffset.UtcNow));
        db.ExternalEntityLinks.Add(new MyIS.Core.Domain.Mdm.Entities.ExternalEntityLink(nameof(MyIS.Core.Domain.Mdm.Entities.UnitOfMeasure), referenced.Id, "Component2020", "Unit", "2", null, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        db.Items.Add(new MyIS.Core.Domain.Mdm.Entities.Item("X1", "X1", "Р”РµС‚Р°Р»СЊ", MyIS.Core.Domain.Mdm.Entities.ItemKind.Component, referenced.Id));
        await db.SaveChangesAsync();

        var deltaReader = new FakeDeltaReader(new[]
        {
            new Component2020Unit { Id = 1, Name = "РЁС‚СѓРєРё", Symbol = "С€С‚.", Code = "796" }
        }, Array.Empty<Component2020Supplier>());

        var cursorRepository = new Component2020SyncCursorRepository(db);

        var service = new Component2020SyncService(
            db,
            new FakeSnapshotReader(),
            deltaReader,
            cursorRepository,
            NullLogger<Component2020SyncService>.Instance,
            PasswordHasher);

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
        unitNames.Should().Contain("РќРµР»СЊР·СЏ СѓРґР°Р»РёС‚СЊ");

        (await db.Component2020SyncErrors.AnyAsync(e => e.EntityType == "UnitOfMeasure"))
            .Should()
            .BeTrue();
    }
}

