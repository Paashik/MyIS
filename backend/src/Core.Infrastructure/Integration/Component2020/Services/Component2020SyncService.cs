using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Common;
using MyIS.Core.Domain.Customers.Entities;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.Services;
using MyIS.Core.Domain.Mdm.ValueObjects;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Domain.Statuses.Entities;
using MyIS.Core.Domain.Users;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public class Component2020SyncService : IComponent2020SyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020SnapshotReader _snapshotReader;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly ILogger<Component2020SyncService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public Component2020SyncService(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        ILogger<Component2020SyncService> logger,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    private sealed class DryRunSequenceState
    {
        public required string Prefix { get; init; }
        public int NextNumber { get; set; }
    }

    private static readonly IReadOnlyDictionary<int, string> DefaultRootGroupAbbreviationById =
        new Dictionary<int, string>
        {
            // Root Groups (Access.Groups.Parent = 0)
            [221] = "CMP", // Покупные комплектующие
            [224] = "MAT", // Сырье и материалы
            [225] = "PRD", // Готовая продукция
            [226] = "SRV", // Работы и услуги
            [184] = "SFG"  // Полуфабрикаты
        };

    private static readonly HashSet<int> RootGroupsWithoutAbbreviation = new()
    {
        183, // УДАЛЕННОЕ
        196  // NO BOM
    };

    private sealed record StatusGroupDefinition(int Kind, string Name, int SortOrder);

    private static readonly IReadOnlyDictionary<int, StatusGroupDefinition> StatusGroupDefinitionsByKind =
        new Dictionary<int, StatusGroupDefinition>
        {
            [0] = new(0, "Статусы компонентов", 0),
            [1] = new(1, "Статусы заказов поставщикам", 1),
            [2] = new(2, "Статусы заказов клиентов", 2),
            [3] = new(3, "Типы заказов клиентов", 3)
        };

    private static bool TryResolveStatusGroupDefinition(int? kind, out StatusGroupDefinition definition)
    {
        if (kind.HasValue && StatusGroupDefinitionsByKind.TryGetValue(kind.Value, out definition))
        {
            return true;
        }

        definition = null!;
        return false;
    }

    private static string ResolveNomenclaturePrefix(
        ItemKind itemKind,
        int? groupId,
        Dictionary<int, int> rootGroupIdByExternalId,
        Dictionary<int, string?> rootAbbreviationByExternalId)
    {
        if (!groupId.HasValue)
        {
            return ItemNomenclature.GetDefaultPrefix(itemKind);
        }

        if (!rootGroupIdByExternalId.TryGetValue(groupId.Value, out var rootGroupId))
        {
            return ItemNomenclature.GetDefaultPrefix(itemKind);
        }

        if (rootAbbreviationByExternalId.TryGetValue(rootGroupId, out var abbreviation)
            && !string.IsNullOrWhiteSpace(abbreviation))
        {
            return abbreviation.Trim().ToUpperInvariant();
        }

        return ItemNomenclature.GetDefaultPrefix(itemKind);
    }

    private static bool IsValidNomenclatureNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var v = value.Trim();
        if (v.Length != 10 || v[3] != '-')
        {
            return false;
        }

        for (var i = 0; i < 3; i++)
        {
            var c = v[i];
            if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9'))
            {
                return false;
            }
        }

        for (var i = 4; i < 10; i++)
        {
            if (v[i] < '0' || v[i] > '9')
            {
                return false;
            }
        }

        return true;
    }

    private async Task<int> GetMaxUsedNomenclatureNumberAsync(string prefix, CancellationToken cancellationToken)
    {
        var values = await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.NomenclatureNo.StartsWith($"{prefix}-"))
            .Select(i => i.NomenclatureNo)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var nomenclatureNo in values)
        {
            if (ItemNomenclature.TryExtractNumericSuffix(nomenclatureNo, prefix, out var current) && current > max)
            {
                max = current;
            }
        }

        return max;
    }

    private async Task<ItemSequence?> FindItemSequenceAsync(ItemKind itemKind, bool forUpdate, CancellationToken cancellationToken)
    {
        var providerName = _dbContext.Database.ProviderName;
        var canLock = forUpdate
                      && providerName == "Npgsql.EntityFrameworkCore.PostgreSQL"
                      && _dbContext.Database.CurrentTransaction != null;

        if (canLock)
        {
            return await _dbContext.ItemSequences
                .FromSqlInterpolated($"SELECT * FROM mdm.item_sequences WHERE \"ItemKind\" = {(int)itemKind} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await _dbContext.ItemSequences.SingleOrDefaultAsync(s => s.ItemKind == itemKind, cancellationToken);
    }

    private async Task<ItemSequence> GetOrCreateSequenceAsync(
        ItemKind itemKind,
        string prefix,
        Dictionary<ItemKind, ItemSequence> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(itemKind, out var cached))
        {
            if (!string.Equals(cached.Prefix, prefix, StringComparison.Ordinal))
            {
                var cachedMaxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                cached.SetPrefixAndNextNumber(prefix, cachedMaxUsed + 1);
            }

            return cached;
        }

        var existing = await FindItemSequenceAsync(itemKind, forUpdate: true, cancellationToken);
        if (existing != null)
        {
            if (!string.Equals(existing.Prefix, prefix, StringComparison.Ordinal))
            {
                var existingMaxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                existing.SetPrefixAndNextNumber(prefix, existingMaxUsed + 1);
            }

            cache[itemKind] = existing;
            return existing;
        }

        var maxUsed = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
        var created = new ItemSequence(itemKind, prefix, maxUsed + 1);
        _dbContext.ItemSequences.Add(created);
        cache[itemKind] = created;
        return created;
    }

    private async Task<string> GenerateNextNomenclatureNoAsync(
        ItemKind itemKind,
        string prefix,
        bool dryRun,
        Dictionary<ItemKind, ItemSequence> sequences,
        Dictionary<ItemKind, DryRunSequenceState> dryRunSequences,
        CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            if (!dryRunSequences.TryGetValue(itemKind, out var state) || !string.Equals(state.Prefix, prefix, StringComparison.Ordinal))
            {
                var existing = await FindItemSequenceAsync(itemKind, forUpdate: false, cancellationToken);
                if (existing != null && string.Equals(existing.Prefix, prefix, StringComparison.Ordinal))
                {
                    state = new DryRunSequenceState { Prefix = prefix, NextNumber = existing.NextNumber };
                }
                else
                {
                    var max = await GetMaxUsedNomenclatureNumberAsync(prefix, cancellationToken);
                    state = new DryRunSequenceState { Prefix = prefix, NextNumber = max + 1 };
                }

                dryRunSequences[itemKind] = state;
            }

            var number = state.NextNumber;
            state.NextNumber++;
            return ItemNomenclature.FormatNomenclatureNo(prefix, number);
        }

        var sequence = await GetOrCreateSequenceAsync(itemKind, prefix, sequences, cancellationToken);
        var next = sequence.NextNumber;
        sequence.IncrementNextNumber();
        return ItemNomenclature.FormatNomenclatureNo(prefix, next);
    }

    private sealed record ItemGroupMappings(
        Dictionary<int, Guid> ItemGroupIdByExternalId,
        Dictionary<int, int> RootGroupIdByExternalId,
        Dictionary<int, string?> RootAbbreviationByExternalId);

    private static int ResolveRootGroupId(int id, Dictionary<int, int?> parentById)
    {
        var visited = new HashSet<int>();
        var current = id;
        const int maxDepth = 100; // Prevent infinite loops
        var depth = 0;

        while (depth < maxDepth)
        {
            if (!visited.Add(current))
            {
                // Circular reference detected, return current as root
                return current;
            }

            if (!parentById.TryGetValue(current, out var parentId) || parentId == null || parentId.Value <= 0)
            {
                return current;
            }

            current = parentId.Value;
            depth++;
        }

        // Max depth reached, return current as root
        return current;
    }

    private static ItemKind MapRootGroupToItemKind(int rootGroupId) =>
        rootGroupId switch
        {
            221 => ItemKind.Component, // Покупные комплектующие
            224 => ItemKind.Material,  // Сырье и материалы
            225 => ItemKind.Product,   // Готовая продукция
            226 => ItemKind.Service,   // Работы и услуги
            184 => ItemKind.Assembly,  // Полуфабрикаты
            _ => ItemKind.Component
        };

    private static ItemKind ResolveItemKindByGroupRoot(int? groupId, Dictionary<int, int> rootGroupIdByExternalId, ItemKind fallback)
    {
        if (!groupId.HasValue)
        {
            return fallback;
        }

        if (rootGroupIdByExternalId.TryGetValue(groupId.Value, out var rootGroupId))
        {
            return MapRootGroupToItemKind(rootGroupId);
        }

        return fallback;
    }

    public async Task<RunComponent2020SyncResponse> RunSyncAsync(RunComponent2020SyncCommand command, CancellationToken cancellationToken)
    {
        var runMode = $"{(command.DryRun ? "DryRun" : "Commit")}:{command.SyncMode}";
        var run = new Component2020SyncRun(command.Scope.ToString(), runMode, command.StartedByUserId);

        _dbContext.Component2020SyncRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var counters = new Dictionary<string, int>();
            var errors = new List<Component2020SyncError>();

            if (command.Scope == Component2020SyncScope.Units || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncUnitsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Counterparties || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncCounterpartiesFromProvidersAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.ItemGroups || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncItemGroupsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Items || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncItemsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Products || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncProductsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Manufacturers || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncManufacturersAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.BodyTypes || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncBodyTypesAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Currencies || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncCurrenciesAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.TechnicalParameters || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncTechnicalParametersAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.ParameterSets || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncParameterSetsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Symbols || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncSymbolsAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Employees || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncEmployeesAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Users || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncUsersAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.CustomerOrders || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncCustomerOrdersAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            if (command.Scope == Component2020SyncScope.Statuses || command.Scope == Component2020SyncScope.All)
            {
                var (processed, errs) = await SyncStatusesAsync(command.ConnectionId, command.DryRun, command.SyncMode, run.Id, counters, errors, cancellationToken);
            }

            var totalProcessed = counters.Values.Sum();
            var totalErrors = errors.Count;

            string status = totalErrors == 0 ? "Success" : (totalProcessed > 0 ? "Partial" : "Failed");

            run.Complete(status, totalProcessed, totalErrors, System.Text.Json.JsonSerializer.Serialize(counters), null);

            if (errors.Count > 0)
            {
                _dbContext.Component2020SyncErrors.AddRange(errors);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RunComponent2020SyncResponse
            {
                RunId = run.Id,
                Status = status,
                ProcessedCount = totalProcessed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Component2020 sync");

            run.Complete("Failed", 0, 1, null, ex.Message);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RunComponent2020SyncResponse
            {
                RunId = run.Id,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncUnitsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Unit";
        const string sourceEntity = "Units";
        const string externalSystem = "Component2020";
        const string externalEntity = "Unit";
        const string linkEntityType = nameof(UnitOfMeasure);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var units = (await _deltaReader.ReadUnitsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();
        _logger.LogInformation(
            "Component2020 Units read {Count} rows (mode={Mode}, lastKey={LastKey})",
            units.Count,
            syncMode,
            lastKey ?? "<full>");

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, UnitOfMeasure> existingUnitsById;

        if (!dryRun && units.Count > 0)
        {
            var externalIds = units.Select(u => u.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var unitIds = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingUnits = await _dbContext.UnitOfMeasures
                .Where(u => unitIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            existingUnitsById = existingUnits.ToDictionary(u => u.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingUnitsById = new Dictionary<Guid, UnitOfMeasure>();
        }

        foreach (var unit in units)
        {
            try
            {
                var (code, name, symbol) = MapUnit(unit);
                var externalId = unit.Id.ToString();
                incomingExternalIds?.Add(externalId);

                UnitOfMeasure? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingUnitsById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var created = new UnitOfMeasure(code, name, symbol);
                        var now = DateTimeOffset.UtcNow;
                        _dbContext.UnitOfMeasures.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.Update(code, name, symbol, true);
                        var now = DateTimeOffset.UtcNow;
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), unit.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing unit {UnitId}", unit.Id);
                var error = new Component2020SyncError(runId, entityType, null, unit.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            if (incomingExternalIds.Count == 0)
            {
                errors.Add(new Component2020SyncError(
                    runId,
                    entityType,
                    null,
                    null,
                    "Overwrite requested, but source returned 0 rows. Deletion is skipped to prevent accidental mass delete.",
                    null));
                counters["UnitDeleted"] = 0;
            }
            else
            {
                var deleted = await DeleteMissingUnitsAsync(incomingExternalIds, runId, errors, cancellationToken);
                counters["UnitDeleted"] = deleted;
            }
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private static (string? code, string name, string symbol) MapUnit(Component2020Unit unit)
    {
        var symbol = (unit.Symbol ?? string.Empty).Trim();
        var name = (unit.Name ?? string.Empty).Trim();
        var code = NormalizeOptional(unit.Code);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = !string.IsNullOrWhiteSpace(symbol) ? symbol : unit.Id.ToString();
        }



        return (code, name, symbol);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncCounterpartiesFromProvidersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Counterparty";
        const string sourceEntity = "Providers";
        const string externalSystem = "Component2020";
        const string externalEntity = "Providers";
        const string linkEntityType = nameof(Counterparty);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var providers = (await _deltaReader.ReadSuppliersDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        _logger.LogInformation(
            "Component2020 Providers read {Count} rows (mode={Mode}, lastKey={LastKey})",
            providers.Count,
            syncMode,
            lastKey ?? "<full>");

        var sw = Stopwatch.StartNew();

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Counterparty> existingCounterpartiesById;
        HashSet<(Guid CounterpartyId, int RoleType)> existingRoleKeys;

        if (!dryRun && providers.Count > 0)
        {
            var providerExternalIds = providers.Select(p => p.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && providerExternalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var linkedCounterpartyIds = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingCounterparties = await _dbContext.Counterparties
                .Where(c => linkedCounterpartyIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            existingCounterpartiesById = existingCounterparties.ToDictionary(c => c.Id);

            var existingRoles = await _dbContext.CounterpartyRoles
                .Where(r => linkedCounterpartyIds.Contains(r.CounterpartyId))
                .Select(r => new { r.CounterpartyId, r.RoleType })
                .ToListAsync(cancellationToken);

            existingRoleKeys = existingRoles.Select(r => (r.CounterpartyId, r.RoleType)).ToHashSet();
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingCounterpartiesById = new Dictionary<Guid, Counterparty>();
            existingRoleKeys = new HashSet<(Guid CounterpartyId, int RoleType)>();
        }

        foreach (var provider in providers)
        {
            try
            {
                var externalId = provider.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var roleType = provider.ProviderType;

                var name = (provider.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Counterparty name cannot be empty.");
                }

                var fullName = NormalizeOptional(provider.FullName);
                var inn = NormalizeOptional(provider.Inn);
                var kpp = NormalizeOptional(provider.Kpp);
                var email = NormalizeOptional(provider.Email);
                var phone = NormalizeOptional(provider.Phone);
                var city = NormalizeOptional(provider.City);
                var address = NormalizeOptional(provider.Address);
                var site = NormalizeOptional(provider.Site);
                var siteLogin = NormalizeOptional(provider.SiteLogin);
                var sitePassword = NormalizeOptional(provider.SitePassword);
                var note = NormalizeOptional(provider.Note);
                var providerType = provider.ProviderType;

                if (!dryRun)
                {
                    var now = DateTimeOffset.UtcNow;

                    existingLinksByExternalId.TryGetValue(externalId, out var existingLink);

                    Counterparty? counterparty = null;

                    if (existingLink != null)
                    {
                        existingCounterpartiesById.TryGetValue(existingLink.EntityId, out counterparty);
                    }

                    if (counterparty == null)
                    {
                        counterparty = new Counterparty(
                            name: name,
                            fullName: fullName,
                            inn: inn,
                            kpp: kpp,
                            email: email,
                            phone: phone,
                            city: city,
                            address: address,
                            site: site,
                            siteLogin: siteLogin,
                            sitePassword: sitePassword,
                            note: note);
                        _dbContext.Counterparties.Add(counterparty);
                    }
                    else
                    {
                        counterparty.UpdateFromExternal(
                            name,
                            fullName,
                            inn,
                            kpp,
                            email,
                            phone,
                            city,
                            address,
                            site,
                            siteLogin,
                            sitePassword,
                            note,
                            true);
                    }

                    if (roleType != 0)
                    {
                        if (!existingRoleKeys.Contains((counterparty.Id, roleType)))
                        {
                            _dbContext.CounterpartyRoles.Add(new CounterpartyRole(counterparty.Id, roleType));
                            existingRoleKeys.Add((counterparty.Id, roleType));
                        }
                        else
                        {
                            // Role exists; ensure it is active.
                            var role = await _dbContext.CounterpartyRoles
                                .FirstOrDefaultAsync(r => r.CounterpartyId == counterparty.Id && r.RoleType == roleType, cancellationToken);
                            role?.Activate();
                        }
                    }

                    EnsureExternalEntityLink(
                        existingLinksByExternalId,
                        linkEntityType,
                        counterparty.Id,
                        externalSystem,
                        externalEntity,
                        externalId,
                        providerType,
                        now);
                }

                processed++;

                var previous = int.TryParse(newLastKey, out var previousId) ? previousId : 0;
                newLastKey = Math.Max(previous, provider.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing provider {ProviderId}", provider.Id);
                var error = new Component2020SyncError(runId, entityType, externalEntity, provider.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingExternalLinksAsync(
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ProviderLinkDeleted"] = deleted;
        }

        counters[entityType] = processed;

        _logger.LogInformation(
            "Component2020 Providers sync finished: processed={Processed}, errors={Errors}, elapsedMs={ElapsedMs}",
            processed,
            errors.Count,
            sw.ElapsedMilliseconds);

        return (processed, errors);
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void EnsureExternalEntityLink(
        Dictionary<string, ExternalEntityLink> linksByExternalId,
        string entityType,
        Guid entityId,
        string externalSystem,
        string externalEntity,
        string externalId,
        int? sourceType,
        DateTimeOffset now)
    {
        if (linksByExternalId.TryGetValue(externalId, out var link))
        {
            if (!string.Equals(link.EntityType, entityType, StringComparison.Ordinal) || link.EntityId != entityId)
            {
                throw new InvalidOperationException(
                    $"External link {externalSystem}:{externalEntity}:{externalId} is already linked to another {link.EntityType}:{link.EntityId}.");
            }

            link.Touch(now, sourceType);
            return;
        }

        var created = new ExternalEntityLink(entityType, entityId, externalSystem, externalEntity, externalId, sourceType, now);
        _dbContext.ExternalEntityLinks.Add(created);
        linksByExternalId[externalId] = created;
    }

    private async Task<ItemGroupMappings> EnsureItemGroupsAsync(Guid connectionId, bool dryRun, CancellationToken cancellationToken)
    {
        const string externalSystem = "Component2020";
        const string externalEntity = "Groups";
        const string linkEntityType = nameof(ItemGroup);

        _logger.LogInformation("Starting Groups import from Component2020 (connectionId={ConnectionId}, dryRun={DryRun})", connectionId, dryRun);

        var groups = (await _snapshotReader.ReadItemGroupsAsync(cancellationToken, connectionId)).ToList();
        _logger.LogInformation("Read {Count} groups from Component2020", groups.Count);

        if (groups.Count == 0)
        {
            _logger.LogWarning("No groups found in Component2020 - returning empty mappings");
            return new ItemGroupMappings(new Dictionary<int, Guid>(), new Dictionary<int, int>(), new Dictionary<int, string?>());
        }

        // Check for duplicates
        var duplicateIds = groups.GroupBy(g => g.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Any())
        {
            _logger.LogWarning("Found duplicate group IDs in Component2020 data: {DuplicateIds}", string.Join(", ", duplicateIds));
            // Remove duplicates, keeping the first occurrence
            groups = groups.GroupBy(g => g.Id).Select(g => g.First()).ToList();
            _logger.LogInformation("Removed duplicates, now have {Count} unique groups", groups.Count);
        }

        var externalIds = groups.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();
        _logger.LogDebug("Processing external IDs: {ExternalIds}", string.Join(", ", externalIds));

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, ItemGroup> existingGroupsById;

        if (!dryRun)
        {
            _logger.LogInformation("Loading existing external links for {Count} groups", externalIds.Count);
            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} existing external links", existingLinks.Count);

            // Handle potential duplicates by taking the most recent link
            existingLinksByExternalId = existingLinks
                .GroupBy(l => l.ExternalId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.SyncedAt).First(), StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            _logger.LogInformation("Loading existing ItemGroups for {Count} IDs", ids.Count);
            var existingEntities = await _dbContext.ItemGroups
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} existing ItemGroups", existingEntities.Count);
            existingGroupsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            _logger.LogInformation("Running in dry mode - skipping database lookups");
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingGroupsById = new Dictionary<Guid, ItemGroup>();
        }

        var groupsByExternalId = new Dictionary<string, ItemGroup>(StringComparer.Ordinal);
        var createdCount = 0;
        var updatedCount = 0;

        foreach (var group in groups)
        {
            try
            {
                var externalId = group.Id.ToString();
                _logger.LogDebug("Processing group {GroupId} - {GroupName}", group.Id, group.Name);

                ItemGroup? existing = null;

                if (!dryRun && existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    _logger.LogDebug("Found existing link for group {GroupId}", group.Id);
                    existingGroupsById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (dryRun)
                    {
                        _logger.LogDebug("Dry run - would create group {GroupId}", group.Id);
                        continue;
                    }

                    _logger.LogInformation("Creating new ItemGroup: {GroupName} (externalId={ExternalId})", group.Name, externalId);
                    var created = new ItemGroup(group.Name, null, group.Description);
                    _dbContext.ItemGroups.Add(created);

                    var now = DateTimeOffset.UtcNow;
                    // If there's an existing link but no corresponding entity, update the link to point to the new entity
                    if (existingLinksByExternalId.TryGetValue(externalId, out var orphanLink))
                    {
                        orphanLink.UpdateEntityId(created.Id, now);
                        _logger.LogDebug("Updated existing orphan link for group {GroupId} to point to new entity", group.Id);
                    }
                    else
                    {
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    groupsByExternalId[externalId] = created;
                    createdCount++;
                    _logger.LogDebug("Successfully created group {GroupId}", group.Id);
                }
                else
                {
                    _logger.LogInformation("Updating existing ItemGroup: {GroupName} (externalId={ExternalId})", group.Name, externalId);
                    if (!dryRun)
                    {
                        existing.Update(group.Name, existing.ParentId, group.Description);
                        var now = DateTimeOffset.UtcNow;
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    groupsByExternalId[externalId] = existing;
                    updatedCount++;
                    _logger.LogDebug("Successfully updated group {GroupId}", group.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing group {GroupId}: {Message}", group.Id, ex.Message);
                throw; // Re-throw to be caught by caller
            }
        }

        _logger.LogInformation("Groups processing completed: created={Created}, updated={Updated}", createdCount, updatedCount);

        if (!dryRun && groupsByExternalId.Count > 0)
        {
            _logger.LogInformation("Saving initial groups to database ({Count} groups to save)", groupsByExternalId.Count);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully saved initial groups to database");

            _logger.LogInformation("Processing parent relationships for {Count} groups", groups.Count);
            var changed = false;
            var parentProcessedCount = 0;
            foreach (var group in groups)
            {
                try
                {
                    var externalId = group.Id.ToString();
                    if (!groupsByExternalId.TryGetValue(externalId, out var entity))
                    {
                        _logger.LogDebug("Group {GroupId} not found in processed groups - skipping parent processing", group.Id);
                        continue;
                    }

                    Guid? desiredParentId = null;
                    if (group.ParentId != null && group.ParentId.Value > 0 && group.ParentId.Value != group.Id)
                    {
                        var parentExternalId = group.ParentId.Value.ToString();
                        _logger.LogDebug("Group {GroupId} has parent {ParentId}", group.Id, group.ParentId.Value);

                        if (!groupsByExternalId.TryGetValue(parentExternalId, out var parent))
                        {
                            _logger.LogWarning("Parent group {ParentId} not found in current batch for group {GroupId} - parent relationship will be set later or in subsequent sync", group.ParentId.Value, group.Id);
                            // Don't set desiredParentId - leave it null for now
                            // The parent might exist in a previous sync or future sync
                        }
                        else
                        {
                            _logger.LogDebug("Found parent group {ParentId} for group {GroupId}", parent.Id, group.Id);
                            desiredParentId = parent.Id;
                        }
                    }

                    if (entity.ParentId != desiredParentId)
                    {
                        _logger.LogInformation("Updating parent for group {GroupId}: {OldParentId} -> {NewParentId}",
                            group.Id, entity.ParentId, desiredParentId);
                        entity.Update(entity.Name, desiredParentId);
                        changed = true;
                    }
                    parentProcessedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing parent relationship for group {GroupId}", group.Id);
                    throw;
                }
            }

            _logger.LogInformation("Processed parent relationships for {ProcessedCount} out of {TotalCount} groups", parentProcessedCount, groups.Count);

            if (changed)
            {
                _logger.LogInformation("Saving parent relationship changes to database");
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Processing abbreviations for root groups");
            var abbreviationChanged = false;
            foreach (var group in groups)
            {
                try
                {
                    if (group.ParentId != null)
                    {
                        continue;
                    }

                    var externalId = group.Id.ToString();
                    if (!groupsByExternalId.TryGetValue(externalId, out var entity))
                    {
                        _logger.LogDebug("Root group {GroupId} not found in processed groups - skipping abbreviation processing", group.Id);
                        continue;
                    }

                    if (RootGroupsWithoutAbbreviation.Contains(group.Id))
                    {
                        if (!string.IsNullOrWhiteSpace(entity.Abbreviation))
                        {
                            _logger.LogInformation("Removing abbreviation from group {GroupId} (in exclusion list)", group.Id);
                            entity.SetAbbreviation(null);
                            abbreviationChanged = true;
                        }

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entity.Abbreviation)
                        && DefaultRootGroupAbbreviationById.TryGetValue(group.Id, out var defaultAbbreviation))
                    {
                        try
                        {
                            _logger.LogInformation("Setting default abbreviation '{Abbreviation}' for group {GroupId}", defaultAbbreviation, group.Id);
                            entity.SetAbbreviation(defaultAbbreviation);
                            abbreviationChanged = true;
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogWarning(ex, "Invalid default abbreviation '{Abbreviation}' for group {GroupId} - skipping", defaultAbbreviation, group.Id);
                            // Continue processing other groups
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing abbreviation for group {GroupId}", group.Id);
                    // Don't throw - continue with other groups to avoid failing the entire import
                }
            }

            if (abbreviationChanged)
            {
                _logger.LogInformation("Saving abbreviation changes to database");
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Creating final mappings for {Count} processed groups", groupsByExternalId.Count);
        
        var itemGroupIdByExternalId = new Dictionary<int, Guid>();
        foreach (var (externalId, entity) in groupsByExternalId)
        {
            if (int.TryParse(externalId, out var id))
            {
                itemGroupIdByExternalId[id] = entity.Id;
            }
        }
        _logger.LogDebug("Created itemGroupIdByExternalId mapping with {Count} entries", itemGroupIdByExternalId.Count);

        var parentById = groups.ToDictionary(x => x.Id, x => x.ParentId);
        _logger.LogDebug("Created parentById mapping with {Count} entries", parentById.Count);
        
        var rootGroupIdByExternalId = new Dictionary<int, int>();
        foreach (var group in groups)
        {
            var rootId = ResolveRootGroupId(group.Id, parentById);
            rootGroupIdByExternalId[group.Id] = rootId;
            _logger.LogDebug("Group {GroupId} -> Root {RootId}", group.Id, rootId);
        }

        var rootAbbreviationByExternalId = new Dictionary<int, string?>();
        foreach (var rootId in rootGroupIdByExternalId.Values.Distinct())
        {
            try
            {
                string? abbreviation = null;
                var externalId = rootId.ToString();
                if (groupsByExternalId.TryGetValue(externalId, out var rootEntity))
                {
                    abbreviation = rootEntity.Abbreviation;
                    _logger.LogDebug("Root group {RootId} has abbreviation from entity: {Abbreviation}", rootId, abbreviation);
                }

                if (string.IsNullOrWhiteSpace(abbreviation) && DefaultRootGroupAbbreviationById.TryGetValue(rootId, out var defaultAbbreviation))
                {
                    abbreviation = defaultAbbreviation;
                    _logger.LogDebug("Root group {RootId} using default abbreviation: {Abbreviation}", rootId, abbreviation);
                }

                if (RootGroupsWithoutAbbreviation.Contains(rootId))
                {
                    abbreviation = null;
                    _logger.LogDebug("Root group {RootId} in exclusion list - no abbreviation", rootId);
                }

                rootAbbreviationByExternalId[rootId] = abbreviation;
                _logger.LogInformation("Final abbreviation for root group {RootId}: {Abbreviation}", rootId, abbreviation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining abbreviation for root group {RootId} - using null", rootId);
                rootAbbreviationByExternalId[rootId] = null;
                // Don't throw - continue processing other root groups
            }
        }

        _logger.LogInformation("Groups import completed successfully. Final mappings: itemGroups={ItemGroupCount}, rootGroups={RootGroupCount}, abbreviations={AbbreviationCount}",
            itemGroupIdByExternalId.Count, rootGroupIdByExternalId.Count, rootAbbreviationByExternalId.Count);

        return new ItemGroupMappings(itemGroupIdByExternalId, rootGroupIdByExternalId, rootAbbreviationByExternalId);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncItemGroupsAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        const string entityType = "ItemGroup";
        const string externalSystem = "Component2020";
        const string externalEntity = "Groups";
        const string linkEntityType = nameof(ItemGroup);

        _logger.LogInformation("Starting ItemGroups sync (connectionId={ConnectionId}, dryRun={DryRun}, syncMode={SyncMode})",
            connectionId, dryRun, syncMode);

        var groups = (await _snapshotReader.ReadItemGroupsAsync(cancellationToken, connectionId)).ToList();
        _logger.LogInformation("Read {Count} groups from Component2020 for sync", groups.Count);
        
        var processed = groups.Count;

        var incomingExternalIds = syncMode == Component2020SyncMode.Overwrite
            ? new HashSet<string>(groups.Select(g => g.Id.ToString()), StringComparer.Ordinal)
            : null;

        await using var transaction = !dryRun ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        try
        {
            _logger.LogInformation("Calling EnsureItemGroupsAsync to process groups");
            await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
            _logger.LogInformation("EnsureItemGroupsAsync completed successfully");

            if (!dryRun && syncMode == Component2020SyncMode.Overwrite && incomingExternalIds != null)
            {
                _logger.LogInformation("Performing overwrite cleanup for missing groups");
                var deleted = await DeleteMissingByExternalLinkAsync(
                    _dbContext.ItemGroups,
                    linkEntityType,
                    externalSystem,
                    externalEntity,
                    incomingExternalIds,
                    runId,
                    entityType,
                    errors,
                    cancellationToken);

                _logger.LogInformation("Overwrite cleanup completed: deleted {Count} groups", deleted);
                counters[$"{entityType}Deleted"] = deleted;
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Transaction committed successfully");
            }

            counters[entityType] = processed;
            _logger.LogInformation("ItemGroups sync completed: processed={Processed}, errors={Errors}", processed, errors.Count);
            return (processed, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ItemGroups sync");
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogInformation("Transaction rolled back due to error");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error rolling back transaction");
                }
            }
            throw;
        }
    }

    private async Task<int> DeleteMissingExternalLinksAsync(
        string entityType,
        string externalSystem,
        string externalEntity,
        HashSet<string> incomingExternalIds,
        Guid runId,
        string errorEntityType,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        var toDeleteKeys = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == entityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (toDeleteKeys.Count == 0)
        {
            return 0;
        }

        var ids = toDeleteKeys.Select(x => x.Id).ToList();
        var toDelete = await _dbContext.ExternalEntityLinks.Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
        _dbContext.ExternalEntityLinks.RemoveRange(toDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return toDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for ExternalEntityLink overwrite cleanup, falling back to per-row deletes");
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var key in toDeleteKeys)
            {
                try
                {
                    var current = await _dbContext.ExternalEntityLinks.FirstOrDefaultAsync(l => l.Id == key.Id, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    _dbContext.ExternalEntityLinks.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete ExternalEntityLink {ExternalId} during overwrite cleanup", key.ExternalId);
                    errors.Add(new Component2020SyncError(
                        runId,
                        errorEntityType,
                        externalEntity,
                        key.ExternalId,
                        "Cannot delete external link during overwrite cleanup.",
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncItemsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Item";
        const string sourceEntity = "Items";
        const string externalSystem = "Component2020";
        const string externalEntity = "Component";
        const string linkEntityType = nameof(Item);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var items = (await _deltaReader.ReadItemsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        await using var transaction = !dryRun ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        var groupMappings = await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
        var itemGroupIdByExternalId = groupMappings.ItemGroupIdByExternalId;
        var rootGroupIdByExternalId = groupMappings.RootGroupIdByExternalId;
        var rootAbbreviationByExternalId = groupMappings.RootAbbreviationByExternalId;
        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);

        var unitExternalIds = items
            .Where(x => x.UnitId.HasValue)
            .Select(x => x.UnitId!.Value.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var unitOfMeasureIdByExternalId = unitExternalIds.Count > 0
            ? (await _dbContext.ExternalEntityLinks
                    .AsNoTracking()
                    .Where(l =>
                        l.EntityType == nameof(UnitOfMeasure)
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == "Unit"
                        && unitExternalIds.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(l => l.ExternalId, l => l.EntityId, StringComparer.Ordinal)
            : new Dictionary<string, Guid>(StringComparer.Ordinal);

        var sequences = new Dictionary<ItemKind, ItemSequence>();
        var dryRunSequences = new Dictionary<ItemKind, DryRunSequenceState>();

        var prefixByKind = new Dictionary<ItemKind, string>();
        var maxIncomingCodeByKind = new Dictionary<ItemKind, int>();
        foreach (var item in items)
        {
            var kind = ResolveItemKindByGroupRoot(item.GroupId, rootGroupIdByExternalId, ItemKind.Component);
            var prefix = ResolveNomenclaturePrefix(kind, item.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);
            var itemCode = NormalizeOptional(item.Code);

            if (!prefixByKind.TryGetValue(kind, out var existingPrefix))
            {
                prefixByKind[kind] = prefix;
            }
            else if (!string.Equals(existingPrefix, prefix, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Multiple nomenclature prefixes detected for ItemKind={ItemKind} during Component2020 sync (keeping '{Prefix}', ignoring '{OtherPrefix}')",
                    kind,
                    existingPrefix,
                    prefix);
            }

            if (ItemNomenclature.TryParseComponentCode(itemCode, out var codeNumber))
            {
                maxIncomingCodeByKind[kind] = Math.Max(maxIncomingCodeByKind.GetValueOrDefault(kind), codeNumber);
            }
        }

        if (!dryRun)
        {
            foreach (var (kind, prefix) in prefixByKind)
            {
                var sequence = await GetOrCreateSequenceAsync(kind, prefix, sequences, cancellationToken);
                if (maxIncomingCodeByKind.TryGetValue(kind, out var maxCodeNumber))
                {
                    sequence.EnsureNextNumberAtLeast(maxCodeNumber + 1);
                }
            }
        }

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLastId) ? parsedLastId : 0;

        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Item> existingItemsById;

        if (!dryRun && items.Count > 0)
        {
            var externalIds = items.Select(i => i.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Items
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingItemsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingItemsById = new Dictionary<Guid, Item>();
        }

        foreach (var item in items)
        {
            try
            {
                var externalId = item.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var unitOfMeasureId = defaultUoM.Id;
                if (item.UnitId.HasValue && unitOfMeasureIdByExternalId.TryGetValue(item.UnitId.Value.ToString(), out var mappedUoMId))
                {
                    unitOfMeasureId = mappedUoMId;
                }

                var itemKind = ResolveItemKindByGroupRoot(item.GroupId, rootGroupIdByExternalId, ItemKind.Component);
                var itemCode = NormalizeOptional(item.Code);

                Guid? itemGroupId = null;
                if (item.GroupId.HasValue && itemGroupIdByExternalId.TryGetValue(item.GroupId.Value, out var mappedGroupId))
                {
                    itemGroupId = mappedGroupId;
                }

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                var prefix = ResolveNomenclaturePrefix(itemKind, item.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);

                string nomenclatureNo;
                if (ItemNomenclature.TryParseComponentCode(itemCode, out var codeNumber))
                {
                    nomenclatureNo = ItemNomenclature.FormatNomenclatureNo(prefix, codeNumber);
                }
                else
                {
                    nomenclatureNo = await GenerateNextNomenclatureNoAsync(itemKind, prefix, dryRun, sequences, dryRunSequences, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var code = itemCode ?? nomenclatureNo;
                        var newItem = new Item(code, nomenclatureNo, item.Name, itemKind, unitOfMeasureId, itemGroupId);
                        newItem.Update(nomenclatureNo, item.Name, unitOfMeasureId, itemGroupId, newItem.IsEskd, newItem.IsEskdDocument, newItem.Designation, item.PartNumber);
                        var now = DateTimeOffset.UtcNow;
                        _dbContext.Items.Add(newItem);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, newItem.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.SetItemKind(itemKind);

                        var targetNomenclatureNo = IsValidNomenclatureNo(existing.NomenclatureNo) ? existing.NomenclatureNo : nomenclatureNo;
                        existing.Update(targetNomenclatureNo, item.Name, unitOfMeasureId, itemGroupId, existing.IsEskd, existing.IsEskdDocument, existing.Designation, item.PartNumber);
                        var now = DateTimeOffset.UtcNow;
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item {ItemId}", item.Id);
                var error = new Component2020SyncError(runId, entityType, null, item.Id.ToString(CultureInfo.InvariantCulture), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            var newLastKey = newLastId.ToString(CultureInfo.InvariantCulture);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Items,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ItemDeleted"] = deleted;
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncProductsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Product";
        const string sourceEntity = "Products";
        const string externalSystem = "Component2020Product";
        const string externalEntity = "Product";
        const string linkEntityType = nameof(Item);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var products = (await _deltaReader.ReadProductsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        await using var transaction = !dryRun ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        var groupMappings = await EnsureItemGroupsAsync(connectionId, dryRun, cancellationToken);
        var itemGroupIdByExternalId = groupMappings.ItemGroupIdByExternalId;
        var rootGroupIdByExternalId = groupMappings.RootGroupIdByExternalId;
        var rootAbbreviationByExternalId = groupMappings.RootAbbreviationByExternalId;
        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);

        var sequences = new Dictionary<ItemKind, ItemSequence>();
        var dryRunSequences = new Dictionary<ItemKind, DryRunSequenceState>();

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLastId) ? parsedLastId : 0;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Item> existingItemsById;

        if (!dryRun && products.Count > 0)
        {
            var externalIds = products.Select(p => p.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Items
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingItemsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingItemsById = new Dictionary<Guid, Item>();
        }

        foreach (var product in products)
        {
            try
            {
                var externalId = product.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var name = string.IsNullOrWhiteSpace(product.Description) ? product.Name : product.Description;
                var designation = product.Name;

                Guid? itemGroupId = null;
                if (product.GroupId.HasValue && itemGroupIdByExternalId.TryGetValue(product.GroupId.Value, out var mappedGroupId))
                {
                    itemGroupId = mappedGroupId;
                }

                var itemKind = ResolveItemKindByGroupRoot(product.GroupId, rootGroupIdByExternalId, ItemKind.Product);

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                var prefix = ResolveNomenclaturePrefix(itemKind, product.GroupId, rootGroupIdByExternalId, rootAbbreviationByExternalId);

                var nomenclatureNo = existing != null && IsValidNomenclatureNo(existing.NomenclatureNo)
                    ? existing.NomenclatureNo
                    : await GenerateNextNomenclatureNoAsync(itemKind, prefix, dryRun, sequences, dryRunSequences, cancellationToken);

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var newItem = new Item(nomenclatureNo, nomenclatureNo, name, itemKind, defaultUoM.Id, itemGroupId);
                        newItem.Update(nomenclatureNo, name, defaultUoM.Id, itemGroupId, newItem.IsEskd, newItem.IsEskdDocument, designation, newItem.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        _dbContext.Items.Add(newItem);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, newItem.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.SetItemKind(itemKind);

                        var targetNomenclatureNo = IsValidNomenclatureNo(existing.NomenclatureNo) ? existing.NomenclatureNo : nomenclatureNo;
                        existing.Update(targetNomenclatureNo, name, defaultUoM.Id, itemGroupId, existing.IsEskd, existing.IsEskdDocument, designation, existing.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing product {ProductId}", product.Id);
                var error = new Component2020SyncError(runId, entityType, null, product.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            var newLastKey = newLastId.ToString(CultureInfo.InvariantCulture);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Items,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ProductDeleted"] = deleted;
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<UnitOfMeasure> FindDefaultUnitOfMeasureAsync(CancellationToken cancellationToken)
    {
        // Backward-compatibility and common variants:
        // - older seed: "pcs"
        // - RU: "шт." / "шт"
        // - OKЕI for pieces: 796 (may be stored in Name)
        var preferred = new[] { "796", "pcs", "шт.", "шт", "pc", "piece" };

        foreach (var code in preferred)
        {
            var found = await _dbContext.UnitOfMeasures
                .FirstOrDefaultAsync(u => u.Code != null && u.Code.ToLower() == code.ToLower(), cancellationToken);
            if (found != null) return found;
        }

        var bySymbol = await _dbContext.UnitOfMeasures
            .FirstOrDefaultAsync(u => u.Symbol.ToLower() == "шт." || u.Symbol.ToLower() == "шт", cancellationToken);
        if (bySymbol != null) return bySymbol;

        return await _dbContext.UnitOfMeasures.FirstAsync(cancellationToken);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncManufacturersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Manufacturer";
        const string sourceEntity = "Manufacturers";
        const string externalSystem = "Component2020";
        const string externalEntity = "Manufact";
        const string linkEntityType = nameof(Manufacturer);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var manufacturers = await _deltaReader.ReadManufacturersDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Manufacturer> existingManufacturersById;

        if (!dryRun)
        {
            var externalIds = manufacturers.Select(m => m.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Manufacturers
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingManufacturersById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingManufacturersById = new Dictionary<Guid, Manufacturer>();
        }

        foreach (var manufacturer in manufacturers)
        {
            try
            {
                var externalId = manufacturer.Id.ToString();
                incomingExternalIds?.Add(externalId);

                Manufacturer? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingManufacturersById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new Manufacturer(manufacturer.Name, manufacturer.FullName, manufacturer.Site, manufacturer.Note);
                        _dbContext.Manufacturers.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(manufacturer.Name, manufacturer.FullName, manufacturer.Site, manufacturer.Note, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), manufacturer.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing manufacturer {ManufacturerId}", manufacturer.Id);
                var error = new Component2020SyncError(runId, entityType, null, manufacturer.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Manufacturers,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ManufacturerDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncBodyTypesAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "BodyType";
        const string sourceEntity = "BodyTypes";
        const string externalSystem = "Component2020";
        const string externalEntity = "Body";
        const string linkEntityType = nameof(BodyType);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var bodyTypes = await _deltaReader.ReadBodyTypesDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, BodyType> existingById;

        if (!dryRun)
        {
            var externalIds = bodyTypes.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.BodyTypes
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingById = new Dictionary<Guid, BodyType>();
        }

        foreach (var bodyType in bodyTypes)
        {
            try
            {
                var externalId = bodyType.Id.ToString();
                incomingExternalIds?.Add(externalId);

                BodyType? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new BodyType(bodyType.Name, bodyType.Name, bodyType.Description, bodyType.Pins, bodyType.Smt, bodyType.Photo, bodyType.FootPrintPath, bodyType.FootprintRef, bodyType.FootprintRef2, bodyType.FootPrintRef3);
                        _dbContext.BodyTypes.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(bodyType.Name, bodyType.Description, bodyType.Pins, bodyType.Smt, bodyType.Photo, bodyType.FootPrintPath, bodyType.FootprintRef, bodyType.FootprintRef2, bodyType.FootPrintRef3, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), bodyType.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing body type {BodyTypeId}", bodyType.Id);
                var error = new Component2020SyncError(runId, entityType, null, bodyType.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.BodyTypes,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["BodyTypeDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncCurrenciesAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Currency";
        const string sourceEntity = "Currencies";
        const string externalSystem = "Component2020";
        const string externalEntity = "Curr";
        const string linkEntityType = nameof(Currency);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var currencies = await _deltaReader.ReadCurrenciesDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Currency> existingById;

        if (!dryRun)
        {
            var externalIds = currencies.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Currencies
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingById = new Dictionary<Guid, Currency>();
        }

        foreach (var currency in currencies)
        {
            try
            {
                var externalId = currency.Id.ToString();
                incomingExternalIds?.Add(externalId);

                // Access: Curr(Code, Name, Symbol, Rate). In MyIS: Code is optional and stored "as is" (may be null).
                var code = NormalizeOptional(currency.Code);
                var symbol = NormalizeOptional(currency.Symbol);

                Currency? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new Currency(code, currency.Name, symbol, currency.Rate);
                        _dbContext.Currencies.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(currency.Name, symbol, currency.Rate, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), currency.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing currency {CurrencyId}", currency.Id);
                var error = new Component2020SyncError(runId, entityType, null, currency.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Currencies,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["CurrencyDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncTechnicalParametersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "TechnicalParameter";
        const string sourceEntity = "TechnicalParameters";
        const string externalSystem = "Component2020";
        const string externalEntity = "NPar";
        const string linkEntityType = nameof(TechnicalParameter);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var technicalParameters = await _deltaReader.ReadTechnicalParametersDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, TechnicalParameter> existingById;

        if (!dryRun)
        {
            var externalIds = technicalParameters.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.TechnicalParameters
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingById = new Dictionary<Guid, TechnicalParameter>();
        }

        foreach (var technicalParameter in technicalParameters)
        {
            try
            {
                var externalId = technicalParameter.Id.ToString();
                incomingExternalIds?.Add(externalId);

                TechnicalParameter? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new TechnicalParameter(technicalParameter.Name, technicalParameter.Name, technicalParameter.Symbol, technicalParameter.UnitId);
                        _dbContext.TechnicalParameters.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(technicalParameter.Name, technicalParameter.Symbol, technicalParameter.UnitId, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), technicalParameter.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing technical parameter {TechnicalParameterId}", technicalParameter.Id);
                var error = new Component2020SyncError(runId, entityType, null, technicalParameter.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.TechnicalParameters,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["TechnicalParameterDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncParameterSetsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "ParameterSet";
        const string sourceEntity = "ParameterSets";
        const string externalSystem = "Component2020";
        const string externalEntity = "SPar";
        const string linkEntityType = nameof(ParameterSet);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var parameterSets = await _deltaReader.ReadParameterSetsDeltaAsync(connectionId, lastKey, cancellationToken);

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, ParameterSet> existingById;

        if (!dryRun)
        {
            var externalIds = parameterSets.Select(x => x.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.ParameterSets
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingById = new Dictionary<Guid, ParameterSet>();
        }

        foreach (var parameterSet in parameterSets)
        {
            try
            {
                var externalId = parameterSet.Id.ToString();
                incomingExternalIds?.Add(externalId);

                ParameterSet? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new ParameterSet(parameterSet.Name, parameterSet.Name, parameterSet.P0Id, parameterSet.P1Id, parameterSet.P2Id, parameterSet.P3Id, parameterSet.P4Id, parameterSet.P5Id);
                        _dbContext.ParameterSets.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(parameterSet.Name, parameterSet.P0Id, parameterSet.P1Id, parameterSet.P2Id, parameterSet.P3Id, parameterSet.P4Id, parameterSet.P5Id, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), parameterSet.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing parameter set {ParameterSetId}", parameterSet.Id);
                var error = new Component2020SyncError(runId, entityType, null, parameterSet.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.ParameterSets,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["ParameterSetDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncSymbolsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Symbol";
        const string sourceEntity = "Symbols";
        const string externalSystem = "Component2020";
        const string externalEntity = "Symbol";
        const string linkEntityType = nameof(Symbol);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var symbols = (await _deltaReader.ReadSymbolsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Symbol> existingSymbolsById;

        if (!dryRun && symbols.Count > 0)
        {
            var externalIds = symbols.Select(s => s.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Symbols
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingSymbolsById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingSymbolsById = new Dictionary<Guid, Symbol>();
        }

        foreach (var symbol in symbols)
        {
            try
            {
                var externalId = symbol.Id.ToString();
                incomingExternalIds?.Add(externalId);

                Symbol? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingSymbolsById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new Symbol(symbol.Name, symbol.Name, symbol.SymbolValue, symbol.Photo, symbol.LibraryPath, symbol.LibraryRef);
                        _dbContext.Symbols.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    // Update if needed
                    if (!dryRun)
                    {
                        existing.Update(symbol.Name, symbol.SymbolValue, symbol.Photo, symbol.LibraryPath, symbol.LibraryRef, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), symbol.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing symbol {SymbolId}", symbol.Id);
                var error = new Component2020SyncError(runId, entityType, null, symbol.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Symbols,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["SymbolDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncEmployeesAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Employee";
        const string sourceEntity = "Person";
        const string externalSystem = "Component2020";
        const string externalEntity = "Person";
        const string linkEntityType = nameof(Employee);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var persons = (await _deltaReader.ReadPersonsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Employee> existingEmployeesById;

        if (!dryRun && persons.Count > 0)
        {
            var externalIds = persons.Select(p => p.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Employees
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingEmployeesById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingEmployeesById = new Dictionary<Guid, Employee>();
        }

        foreach (var person in persons)
        {
            try
            {
                var externalId = person.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var fullName = BuildPersonFullName(person);
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = $"Person {person.Id}";
                }

                var email = NormalizeOptional(person.Email);
                var phone = NormalizeOptional(person.Phone);
                var notes = NormalizeOptional(person.Note);

                Employee? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingEmployeesById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = Employee.Create(Guid.NewGuid(), fullName, email, phone, notes, now);
                        if (person.Hidden)
                        {
                            created.Deactivate(now);
                        }

                        _dbContext.Employees.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        existing.Update(fullName, email, phone, notes, now);
                        if (person.Hidden)
                        {
                            existing.Deactivate(now);
                        }
                        else
                        {
                            existing.Activate(now);
                        }

                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                var previous = int.TryParse(newLastKey, out var previousId) ? previousId : 0;
                newLastKey = Math.Max(previous, person.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing person {PersonId}", person.Id);
                var error = new Component2020SyncError(runId, entityType, null, person.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Employees,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["EmployeeDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncUsersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "User";
        const string sourceEntity = "Users";
        const string externalSystem = "Component2020";
        const string externalEntity = "Users";
        const string linkEntityType = nameof(User);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var users = (await _deltaReader.ReadUsersDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, User> existingUsersById;

        if (!dryRun && users.Count > 0)
        {
            var externalIds = users.Select(u => u.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Users
                .Include(u => u.UserRoles)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingUsersById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingUsersById = new Dictionary<Guid, User>();
        }

        var accessRoles = await _deltaReader.ReadRolesAsync(connectionId, cancellationToken);
        var accessRoleNameById = accessRoles
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .ToDictionary(
                r => r.Id,
                r => string.IsNullOrWhiteSpace(r.Code) ? r.Name : r.Code);

        var roles = await _dbContext.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var roleByCode = roles
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .GroupBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(r => r.Code, StringComparer.OrdinalIgnoreCase);
        var roleByName = roles
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var personExternalIds = users
            .Where(u => u.PersonId.HasValue)
            .Select(u => u.PersonId!.Value.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var employeeIdByExternalPersonId = new Dictionary<int, Guid>();
        if (personExternalIds.Count > 0)
        {
            var personLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == nameof(Employee)
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == "Person"
                    && personExternalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            foreach (var link in personLinks)
            {
                if (int.TryParse(link.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var personId))
                {
                    employeeIdByExternalPersonId[personId] = link.EntityId;
                }
            }
        }

        var employeeIds = employeeIdByExternalPersonId.Values.Distinct().ToList();
        var employeeNameById = employeeIds.Count > 0
            ? await _dbContext.Employees
                .Where(e => employeeIds.Contains(e.Id))
                .Select(e => new { e.Id, e.FullName })
                .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        foreach (var accessUser in users)
        {
            try
            {
                var externalId = accessUser.Id.ToString();
                incomingExternalIds?.Add(externalId);

                var login = NormalizeOptional(accessUser.Name) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(login))
                {
                    throw new ArgumentException("User login cannot be empty.");
                }

                var isActive = !accessUser.Hidden;

                Guid? employeeId = null;
                if (accessUser.PersonId.HasValue && employeeIdByExternalPersonId.TryGetValue(accessUser.PersonId.Value, out var resolvedEmployeeId))
                {
                    employeeId = resolvedEmployeeId;
                }

                employeeNameById.TryGetValue(employeeId ?? Guid.Empty, out var employeeFullName);
                var fullName = string.IsNullOrWhiteSpace(employeeFullName) ? null : employeeFullName;

                var roleIds = ResolveRoleIds(accessUser, accessRoleNameById, roleByCode, roleByName);

                User? existing = null;
                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingUsersById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    var existingByLogin = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
                    if (existingByLogin != null)
                    {
                        existing = existingByLogin;
                    }
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var password = string.IsNullOrWhiteSpace(accessUser.Password) ? login : accessUser.Password;
                        var passwordHash = _passwordHasher.HashPassword(password);

                        var created = User.Create(Guid.NewGuid(), login, passwordHash, isActive, employeeId, now, fullName);
                        _dbContext.Users.Add(created);
                        await ReplaceUserRolesAsync(created.Id, roleIds, now, cancellationToken);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        existing.UpdateDetails(login, isActive, employeeId, now, fullName);
                        if (!string.IsNullOrWhiteSpace(accessUser.Password))
                        {
                            var passwordHash = _passwordHasher.HashPassword(accessUser.Password);
                            existing.ResetPasswordHash(passwordHash, now);
                        }

                        await ReplaceUserRolesAsync(existing.Id, roleIds, now, cancellationToken);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                var previous = int.TryParse(newLastKey, out var previousId) ? previousId : 0;
                newLastKey = Math.Max(previous, accessUser.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user {UserId}", accessUser.Id);
                var error = new Component2020SyncError(runId, entityType, null, accessUser.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Users,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["UserDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncCustomerOrdersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "CustomerOrder";
        const string sourceEntity = "CustomerOrder";
        const string externalSystem = "Component2020";
        const string externalEntity = "CustomerOrder";
        const string linkEntityType = nameof(CustomerOrder);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var orders = (await _deltaReader.ReadCustomerOrdersDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        string? newLastKey = lastKey;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, CustomerOrder> existingOrdersById;

        if (!dryRun && orders.Count > 0)
        {
            var externalIds = orders.Select(o => o.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.CustomerOrders
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingOrdersById = existingEntities.ToDictionary(x => x.Id);
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingOrdersById = new Dictionary<Guid, CustomerOrder>();
        }

        var customerExternalIds = orders
            .Where(o => o.CustomerId.HasValue)
            .Select(o => o.CustomerId!.Value.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var customerIdByExternalId = new Dictionary<int, Guid>();
        if (customerExternalIds.Count > 0)
        {
            var customerLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == nameof(Counterparty)
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == "Providers"
                    && customerExternalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            foreach (var link in customerLinks)
            {
                if (int.TryParse(link.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var externalId))
                {
                    customerIdByExternalId[externalId] = link.EntityId;
                }
            }
        }

        var personExternalIds = orders
            .Where(o => o.PersonId.HasValue)
            .Select(o => o.PersonId!.Value.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var employeeIdByExternalId = new Dictionary<int, Guid>();
        if (personExternalIds.Count > 0)
        {
            var employeeLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == nameof(Employee)
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == "Person"
                    && personExternalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            foreach (var link in employeeLinks)
            {
                if (int.TryParse(link.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var externalId))
                {
                    employeeIdByExternalId[externalId] = link.EntityId;
                }
            }
        }

        foreach (var order in orders)
        {
            try
            {
                var externalId = order.Id.ToString();
                incomingExternalIds?.Add(externalId);

                Guid? customerId = null;
                if (order.CustomerId.HasValue && customerIdByExternalId.TryGetValue(order.CustomerId.Value, out var resolvedCustomerId))
                {
                    customerId = resolvedCustomerId;
                }
                else if (order.CustomerId.HasValue)
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Customer {order.CustomerId.Value} not found.", null));
                }

                Guid? personId = null;
                if (order.PersonId.HasValue && employeeIdByExternalId.TryGetValue(order.PersonId.Value, out var resolvedPersonId))
                {
                    personId = resolvedPersonId;
                }
                else if (order.PersonId.HasValue)
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Person {order.PersonId.Value} not found.", null));
                }

                CustomerOrder? existing = null;
                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingOrdersById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var created = new CustomerOrder(
                            order.Number,
                            order.OrderDate,
                            order.DeliveryDate,
                            order.State,
                            customerId,
                            personId,
                            order.Note,
                            order.Contract,
                            order.StoreId,
                            order.Path,
                            order.PayDate,
                            order.FinishedDate,
                            order.ContactId,
                            order.Discount,
                            order.Tax,
                            order.Mark,
                            order.Pn,
                            order.PaymentForm,
                            order.PayMethod,
                            order.PayPeriod,
                            order.Prepayment,
                            order.Kind,
                            order.AccountId);
                        _dbContext.CustomerOrders.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.UpdateFromExternal(
                            order.Number,
                            order.OrderDate,
                            order.DeliveryDate,
                            order.State,
                            customerId,
                            personId,
                            order.Note,
                            order.Contract,
                            order.StoreId,
                            order.Path,
                            order.PayDate,
                            order.FinishedDate,
                            order.ContactId,
                            order.Discount,
                            order.Tax,
                            order.Mark,
                            order.Pn,
                            order.PaymentForm,
                            order.PayMethod,
                            order.PayPeriod,
                            order.Prepayment,
                            order.Kind,
                            order.AccountId);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                    }
                    processed++;
                }

                var previous = int.TryParse(newLastKey, out var previousId) ? previousId : 0;
                newLastKey = Math.Max(previous, order.Id).ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing customer order {CustomerOrderId}", order.Id);
                var error = new Component2020SyncError(runId, entityType, null, order.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastKey, cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.CustomerOrders,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["CustomerOrderDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncStatusesAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Status";
        const string sourceEntity = "Status";
        const string externalSystem = "Component2020";
        const string externalEntity = "Status";
        const string statusCodeExternalEntity = "StatusCode";
        const string linkEntityType = nameof(Status);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var statuses = (await _deltaReader.ReadStatusesDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        var newLastId = int.TryParse(lastKey, NumberStyles.None, CultureInfo.InvariantCulture, out var lastIdValue) ? lastIdValue : 0;
        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<string, ExternalEntityLink> existingCodeLinksByExternalId;
        Dictionary<Guid, Status> existingStatusesById;
        var statusGroupsByKind = new Dictionary<int, Status>();
        var statusGroupLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);

        if (!dryRun && statuses.Count > 0)
        {
            var externalIds = statuses.Select(s => s.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.ExternalEntityLinks
                .Where(l =>
                    l.EntityType == linkEntityType
                    && l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && externalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var statusCodeTokens = statuses
                .Where(s => s.Code.HasValue)
                .Select(s => s.Code!.Value.ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (statusCodeTokens.Count > 0)
            {
                var existingCodeLinks = await _dbContext.ExternalEntityLinks
                    .Where(l =>
                        l.EntityType == linkEntityType
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == statusCodeExternalEntity
                        && statusCodeTokens.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken);

                existingCodeLinksByExternalId = existingCodeLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);
            }
            else
            {
                existingCodeLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            }

            var ids = existingLinks.Select(l => l.EntityId).Distinct().ToList();
            var existingEntities = await _dbContext.Statuses
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            existingStatusesById = existingEntities.ToDictionary(x => x.Id);

            var requiredKinds = statuses
                .Where(s => s.Kind.HasValue)
                .Select(s => s.Kind!.Value)
                .Distinct()
                .ToList();

            if (requiredKinds.Count > 0)
            {
                var requiredKindTokens = requiredKinds
                    .Select(k => k.ToString(CultureInfo.InvariantCulture))
                    .ToList();

                var existingGroupLinks = await _dbContext.ExternalEntityLinks
                    .Where(l =>
                        l.EntityType == linkEntityType
                        && l.ExternalSystem == externalSystem
                        && l.ExternalEntity == "StatusKind"
                        && requiredKindTokens.Contains(l.ExternalId))
                    .ToListAsync(cancellationToken);

                statusGroupLinksByExternalId = existingGroupLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

                var groupIds = existingGroupLinks
                    .Select(x => x.EntityId)
                    .Distinct()
                    .ToList();

                if (groupIds.Count > 0)
                {
                    var existingGroups = await _dbContext.Statuses
                        .Where(x => groupIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var group in existingGroups)
                    {
                        var link = existingGroupLinks.FirstOrDefault(x => x.EntityId == group.Id);
                        if (link == null)
                        {
                            continue;
                        }

                        if (int.TryParse(link.ExternalId, NumberStyles.None, CultureInfo.InvariantCulture, out var kind))
                        {
                            statusGroupsByKind[kind] = group;
                        }
                    }
                }

                foreach (var definition in StatusGroupDefinitionsByKind.Values)
                {
                    if (!requiredKinds.Contains(definition.Kind))
                    {
                        continue;
                    }

                    if (!statusGroupsByKind.TryGetValue(definition.Kind, out var group))
                    {
                        group = new Status(null, definition.Name, description: null, color: null, flags: null, sortOrder: definition.SortOrder);
                        _dbContext.Statuses.Add(group);
                        statusGroupsByKind[definition.Kind] = group;
                    }
                    else
                    {
                        group.UpdateFromExternal(definition.Name, group.Description, group.Color, group.Flags, definition.SortOrder, true);
                    }

                    var now = DateTimeOffset.UtcNow;
                    EnsureExternalEntityLink(
                        statusGroupLinksByExternalId,
                        linkEntityType,
                        group.Id,
                        externalSystem,
                        "StatusKind",
                        definition.Kind.ToString(CultureInfo.InvariantCulture),
                        null,
                        now);
                }
            }
        }
        else
        {
            existingLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingCodeLinksByExternalId = new Dictionary<string, ExternalEntityLink>(StringComparer.Ordinal);
            existingStatusesById = new Dictionary<Guid, Status>();
        }

        foreach (var status in statuses)
        {
            try
            {
                var externalId = status.Id.ToString();
                incomingExternalIds?.Add(externalId);

                if (!TryResolveStatusGroupDefinition(status.Kind, out var definition))
                {
                    errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Unknown status kind '{status.Kind}'.", null));
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(status.Name) ? $"Status {status.Id}" : status.Name.Trim();

                Status? existing = null;
                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingStatusesById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        if (!statusGroupsByKind.TryGetValue(definition.Kind, out var group))
                        {
                            errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Missing status group for kind '{definition.Kind}'.", null));
                            continue;
                        }

                        var created = new Status(group.Id, name, description: null, status.Color, status.Flags, status.SortOrder);
                        _dbContext.Statuses.Add(created);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                        if (status.Code.HasValue)
                        {
                            var codeExternalId = status.Code.Value.ToString(CultureInfo.InvariantCulture);
                            EnsureExternalEntityLink(existingCodeLinksByExternalId, linkEntityType, created.Id, externalSystem, statusCodeExternalEntity, codeExternalId, null, DateTimeOffset.UtcNow);
                        }
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        if (!statusGroupsByKind.TryGetValue(definition.Kind, out var group))
                        {
                            errors.Add(new Component2020SyncError(runId, entityType, null, externalId, $"Missing status group for kind '{definition.Kind}'.", null));
                            continue;
                        }

                        existing.ChangeGroup(group.Id);
                        existing.UpdateFromExternal(name, existing.Description, status.Color, status.Flags, status.SortOrder, true);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
                        if (status.Code.HasValue)
                        {
                            var codeExternalId = status.Code.Value.ToString(CultureInfo.InvariantCulture);
                            EnsureExternalEntityLink(existingCodeLinksByExternalId, linkEntityType, existing.Id, externalSystem, statusCodeExternalEntity, codeExternalId, null, DateTimeOffset.UtcNow);
                        }
                    }
                    processed++;
                }

                newLastId = Math.Max(newLastId, status.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing status {StatusId}", status.Id);
                var error = new Component2020SyncError(runId, entityType, null, status.Id.ToString(), ex.Message, ex.StackTrace);
                errors.Add(error);
            }
        }

        if (!dryRun && processed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cursorRepository.UpsertCursorAsync(connectionId, sourceEntity, newLastId.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        if (!dryRun && isOverwrite && incomingExternalIds != null)
        {
            var deleted = await DeleteMissingByExternalLinkAsync(
                _dbContext.Statuses,
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                runId,
                entityType,
                errors,
                cancellationToken);
            counters["StatusDeleted"] = deleted;
        }

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.UserRoles.RemoveRange(existing);
        }

        if (roleIds.Count == 0)
        {
            return;
        }

        var newLinks = roleIds.Select(roleId => new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = now,
            CreatedAt = now
        });

        await _dbContext.UserRoles.AddRangeAsync(newLinks, cancellationToken);
    }

    private static IReadOnlyCollection<Guid> ResolveRoleIds(
        Component2020User accessUser,
        Dictionary<int, string> accessRoleNameById,
        Dictionary<string, Role> roleByCode,
        Dictionary<string, Role> roleByName)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (accessUser.RoleId.HasValue && accessRoleNameById.TryGetValue(accessUser.RoleId.Value, out var roleToken))
        {
            tokens.Add(roleToken);
        }

        if (!string.IsNullOrWhiteSpace(accessUser.Roles))
        {
            var parts = accessUser.Roles
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    tokens.Add(trimmed);
                }
            }
        }

        if (tokens.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var result = new HashSet<Guid>();
        foreach (var token in tokens)
        {
            if (roleByCode.TryGetValue(token, out var roleByCodeMatch))
            {
                result.Add(roleByCodeMatch.Id);
                continue;
            }

            if (roleByName.TryGetValue(token, out var roleByNameMatch))
            {
                result.Add(roleByNameMatch.Id);
            }
        }

        return result.ToArray();
    }

    private static string BuildPersonFullName(Component2020Person person)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(person.LastName)) parts.Add(person.LastName.Trim());
        if (!string.IsNullOrWhiteSpace(person.FirstName)) parts.Add(person.FirstName.Trim());
        if (!string.IsNullOrWhiteSpace(person.SecondName)) parts.Add(person.SecondName.Trim());

        return string.Join(" ", parts);
    }

    private async Task<int> DeleteMissingUnitsAsync(HashSet<string> incomingExternalIds, Guid runId, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "UnitOfMeasure";
        const string linkEntityType = nameof(UnitOfMeasure);
        const string externalSystem = "Component2020";
        const string externalEntity = "Unit";

        var missingLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.EntityId, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (missingLinks.Count == 0)
        {
            return 0;
        }

        var externalIdByEntityId = new Dictionary<Guid, string>();
        foreach (var link in missingLinks)
        {
            if (!externalIdByEntityId.ContainsKey(link.EntityId))
            {
                externalIdByEntityId[link.EntityId] = link.ExternalId;
            }
        }

        var affectedEntityIds = missingLinks.Select(x => x.EntityId).Distinct().ToList();

        var allLinksForAffectedEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && affectedEntityIds.Contains(l.EntityId))
            .Select(l => new { l.Id, l.EntityId })
            .ToListAsync(cancellationToken);

        var missingLinkIds = missingLinks.Select(x => x.Id).ToHashSet();

        var entityIdsToDelete = allLinksForAffectedEntities
            .GroupBy(x => x.EntityId)
            .Where(g => g.All(x => missingLinkIds.Contains(x.Id)))
            .Select(g => g.Key)
            .ToHashSet();

        var referencedByItems = await _dbContext.Items
            .Where(i => affectedEntityIds.Contains(i.UnitOfMeasureId))
            .Select(i => i.UnitOfMeasureId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referencedByLines = await _dbContext.RequestLines
            .Where(l => l.UnitOfMeasureId != null && affectedEntityIds.Contains(l.UnitOfMeasureId.Value))
            .Select(l => l.UnitOfMeasureId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referenced = referencedByItems.Concat(referencedByLines).ToHashSet();

        if (referenced.Count > 0)
        {
            foreach (var unitId in referenced)
            {
                externalIdByEntityId.TryGetValue(unitId, out var externalId);
                errors.Add(new Component2020SyncError(
                    runId,
                    entityType,
                    null,
                    externalId ?? unitId.ToString(),
                    "Cannot delete unit because it is referenced by existing documents/items.",
                    null));
            }

            foreach (var unitId in referenced)
            {
                entityIdsToDelete.Remove(unitId);
            }
        }

        var linkIdsToDeleteOnly = missingLinks
            .Where(x => !entityIdsToDelete.Contains(x.EntityId))
            .Where(x => !referenced.Contains(x.EntityId))
            .Select(x => x.Id)
            .ToList();

        if (linkIdsToDeleteOnly.Count > 0)
        {
            var links = await _dbContext.ExternalEntityLinks
                .Where(l => linkIdsToDeleteOnly.Contains(l.Id))
                .ToListAsync(cancellationToken);

            _dbContext.ExternalEntityLinks.RemoveRange(links);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (entityIdsToDelete.Count == 0)
        {
            return 0;
        }

        var toDeleteIds = entityIdsToDelete.ToList();
        var linksToDeleteWithEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && toDeleteIds.Contains(l.EntityId))
            .ToListAsync(cancellationToken);

        var unitsToDelete = await _dbContext.UnitOfMeasures
            .Where(u => toDeleteIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(linksToDeleteWithEntities);
        _dbContext.UnitOfMeasures.RemoveRange(unitsToDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return unitsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for UnitOfMeasure overwrite cleanup, falling back to per-row deletes");
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var unitId in toDeleteIds)
            {
                try
                {
                    var current = await _dbContext.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    var currentLinks = await _dbContext.ExternalEntityLinks
                        .Where(l => l.EntityType == linkEntityType && l.EntityId == unitId)
                        .ToListAsync(cancellationToken);
                    _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                    _dbContext.UnitOfMeasures.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete UnitOfMeasure {UnitId} during overwrite cleanup", unitId);
                    externalIdByEntityId.TryGetValue(unitId, out var externalId);
                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        externalId ?? unitId.ToString(),
                        "Cannot delete unit during overwrite cleanup.",
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }

    private async Task<int> DeleteMissingByExternalLinkAsync<TEntity>(
        DbSet<TEntity> set,
        string linkEntityType,
        string externalSystem,
        string externalEntity,
        HashSet<string> incomingExternalIds,
        Guid runId,
        string entityType,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var missingLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.EntityId, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (missingLinks.Count == 0)
        {
            return 0;
        }

        var externalIdByEntityId = new Dictionary<Guid, string>();
        foreach (var link in missingLinks)
        {
            if (!externalIdByEntityId.ContainsKey(link.EntityId))
            {
                externalIdByEntityId[link.EntityId] = link.ExternalId;
            }
        }

        var affectedEntityIds = missingLinks.Select(x => x.EntityId).Distinct().ToList();
        var missingLinkIds = missingLinks.Select(x => x.Id).ToHashSet();

        var allLinksForAffectedEntities = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && affectedEntityIds.Contains(l.EntityId))
            .Select(l => new { l.Id, l.EntityId })
            .ToListAsync(cancellationToken);

        var entityIdsToDeleteByLinks = allLinksForAffectedEntities
            .GroupBy(x => x.EntityId)
            .Where(g => g.All(x => missingLinkIds.Contains(x.Id)))
            .Select(g => g.Key)
            .ToHashSet();

        var entityIdsToDelete = entityIdsToDeleteByLinks;

        var linkIdsToDeleteOnly = missingLinks
            .Where(x => !entityIdsToDelete.Contains(x.EntityId))
            .Select(x => x.Id)
            .ToList();

        if (linkIdsToDeleteOnly.Count > 0)
        {
            var links = await _dbContext.ExternalEntityLinks
                .Where(l => linkIdsToDeleteOnly.Contains(l.Id))
                .ToListAsync(cancellationToken);

            _dbContext.ExternalEntityLinks.RemoveRange(links);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (entityIdsToDelete.Count == 0)
        {
            return 0;
        }

        var ids = entityIdsToDelete.ToList();
        var linksToDeleteWithEntity = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && ids.Contains(l.EntityId))
            .ToListAsync(cancellationToken);
        var entitiesToDelete = await set
            .Where(e => ids.Contains(EF.Property<Guid>(e, "Id")))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(linksToDeleteWithEntity);
        set.RemoveRange(entitiesToDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return entitiesToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for {EntityType} overwrite cleanup, falling back to per-row deletes", entityType);
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var entityId in ids)
            {
                try
                {
                    var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, cancellationToken);

                    var currentLinks = await _dbContext.ExternalEntityLinks
                        .Where(l => l.EntityType == linkEntityType && l.EntityId == entityId)
                        .ToListAsync(cancellationToken);

                    if (current == null)
                    {
                        // Orphan links only
                        _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                    set.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete {EntityType} {EntityId} during overwrite cleanup", entityType, entityId);

                    var message = "Cannot delete record during overwrite cleanup.";

                    try
                    {
                        var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == entityId, cancellationToken);
                        if (current != null)
                        {
                            if (current is IDeactivatable deactivatable)
                            {
                                deactivatable.Deactivate();
                                set.Update(current);
                                await _dbContext.SaveChangesAsync(cancellationToken);
                                message = "Record is referenced; deactivated instead of deleting during overwrite cleanup.";
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    externalIdByEntityId.TryGetValue(entityId, out var externalId);
                    externalId ??= entityId.ToString();

                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        externalId,
                        message,
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }
}
