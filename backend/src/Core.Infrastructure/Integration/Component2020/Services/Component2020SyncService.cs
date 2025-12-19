using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.ValueObjects;
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

    public Component2020SyncService(
        AppDbContext dbContext,
        IComponent2020SnapshotReader snapshotReader,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        ILogger<Component2020SyncService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                // Backward-compatibility: try legacy fields (ExternalSystem/ExternalId) to link older data.
                if (existing == null)
                {
                    existing = await _dbContext.UnitOfMeasures
                        .FirstOrDefaultAsync(u => u.ExternalSystem == externalSystem && u.ExternalId == externalId, cancellationToken);
                }

                // Backward-compatibility: link legacy records (without external keys) by unique Name.
                if (existing == null && !string.IsNullOrWhiteSpace(name))
                {
                    existing = await _dbContext.UnitOfMeasures
                        .FirstOrDefaultAsync(u => u.Name == name, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var created = new UnitOfMeasure(code, name, symbol);
                        var now = DateTimeOffset.UtcNow;
                        created.SetExternalReference(externalSystem, externalId, now);
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
                        existing.SetExternalReference(externalSystem, externalId, now);
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
        var numericCode = (unit.Code ?? string.Empty).Trim();
        var name = (unit.Name ?? string.Empty).Trim();

        string? code = null;
        if (!string.IsNullOrWhiteSpace(numericCode))
        {
            code = numericCode.Length > 10 ? numericCode[..10] : numericCode;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = !string.IsNullOrWhiteSpace(symbol) ? symbol : (code ?? unit.Id.ToString());
        }

        // Preserve the Access "Code" (often numeric / OKЕI) in the Name for now.

        // If we ended up using numeric code (no Symbol) — show Symbol in the name.

        return (code, name, symbol);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncCounterpartiesFromProvidersAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Counterparty";
        const string sourceEntity = "Providers";
        const string externalSystem = "Component2020";
        const string externalEntity = "Providers";

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

        Dictionary<string, CounterpartyExternalLink> existingLinksByExternalId;
        Dictionary<Guid, Counterparty> existingCounterpartiesById;
        HashSet<(Guid CounterpartyId, int RoleType)> existingRoleKeys;

        if (!dryRun && providers.Count > 0)
        {
            var providerExternalIds = providers.Select(p => p.Id.ToString()).Distinct(StringComparer.Ordinal).ToList();

            var existingLinks = await _dbContext.CounterpartyExternalLinks
                .Where(l =>
                    l.ExternalSystem == externalSystem
                    && l.ExternalEntity == externalEntity
                    && providerExternalIds.Contains(l.ExternalId))
                .ToListAsync(cancellationToken);

            existingLinksByExternalId = existingLinks.ToDictionary(l => l.ExternalId, StringComparer.Ordinal);

            var linkedCounterpartyIds = existingLinks.Select(l => l.CounterpartyId).Distinct().ToList();
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
            existingLinksByExternalId = new Dictionary<string, CounterpartyExternalLink>(StringComparer.Ordinal);
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
                        existingCounterpartiesById.TryGetValue(existingLink.CounterpartyId, out counterparty);
                    }

                    if (counterparty == null)
                    {
                        counterparty = new Counterparty(
                            code: null,
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

                    if (existingLink == null)
                    {
                        var link = new CounterpartyExternalLink(
                            counterparty.Id,
                            externalSystem,
                            externalEntity,
                            externalId,
                            providerType,
                            now);

                        _dbContext.CounterpartyExternalLinks.Add(link);
                        existingLinksByExternalId[externalId] = link;
                    }
                    else
                    {
                        if (existingLink.CounterpartyId != counterparty.Id)
                        {
                            throw new InvalidOperationException(
                                $"External link {externalSystem}:{externalEntity}:{externalId} is already linked to another counterparty.");
                        }

                        existingLink.Touch(now, providerType);
                    }
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
            var deleted = await DeleteMissingCounterpartyExternalLinksAsync(
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

    private async Task<int> DeleteMissingCounterpartyExternalLinksAsync(
        string externalSystem,
        string externalEntity,
        HashSet<string> incomingExternalIds,
        Guid runId,
        string entityType,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
    {
        var toDeleteKeys = await _dbContext.CounterpartyExternalLinks
            .Where(l =>
                l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (toDeleteKeys.Count == 0)
        {
            return 0;
        }

        var ids = toDeleteKeys.Select(x => x.Id).ToList();
        var toDelete = await _dbContext.CounterpartyExternalLinks.Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
        _dbContext.CounterpartyExternalLinks.RemoveRange(toDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return toDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for CounterpartyExternalLink overwrite cleanup, falling back to per-row deletes");
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var key in toDeleteKeys)
            {
                try
                {
                    var current = await _dbContext.CounterpartyExternalLinks.FirstOrDefaultAsync(l => l.Id == key.Id, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    _dbContext.CounterpartyExternalLinks.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete CounterpartyExternalLink {ExternalId} during overwrite cleanup", key.ExternalId);
                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
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

        int processed = 0;
        string? newLastKey = lastKey;

        var incomingExternalIds = isOverwrite ? new HashSet<string>(StringComparer.Ordinal) : null;

        Dictionary<string, ExternalEntityLink> existingLinksByExternalId;
        Dictionary<Guid, Item> existingItemsById;

        if (!dryRun && items.Count > 0)
        {
            var externalIds = items.Select(i => i.Code).Distinct(StringComparer.Ordinal).ToList();

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
                var externalId = item.Code;
                incomingExternalIds?.Add(externalId);

                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                // Backward-compatibility: try legacy fields (ExternalSystem/ExternalId) to link older data.
                if (existing == null)
                {
                    existing = await _dbContext.Items
                        .FirstOrDefaultAsync(i => i.ExternalSystem == externalSystem && i.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        // Need UnitOfMeasureId, assume default or find
                        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);
                        var newItem = new Item(item.Code, item.Name, ItemKind.Component, defaultUoM.Id);
                        var now = DateTimeOffset.UtcNow;
                        newItem.SetExternalReference(externalSystem, externalId, now);
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
                        existing.Update(item.Name, existing.UnitOfMeasureId, existing.ItemGroupId, existing.IsEskd, existing.IsEskdDocument, existing.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        existing.SetExternalReference(externalSystem, externalId, now);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastKey = item.Code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item {ItemCode}", item.Code);
                var error = new Component2020SyncError(runId, entityType, null, item.Code, ex.Message, ex.StackTrace);
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

        counters[entityType] = processed;
        return (processed, errors);
    }

    private async Task<(int processed, List<Component2020SyncError> errors)> SyncProductsAsync(Guid connectionId, bool dryRun, Component2020SyncMode syncMode, Guid runId, Dictionary<string, int> counters, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "Product";
        const string sourceEntity = "Products";
        const string externalSystem = "Component2020Product";
        const string legacyExternalSystem = "Component2020";
        const string externalEntity = "Product";
        const string linkEntityType = nameof(Item);

        var isFull = syncMode != Component2020SyncMode.Delta;
        var isOverwrite = syncMode == Component2020SyncMode.Overwrite;

        var lastKey = isFull ? null : await _cursorRepository.GetLastProcessedKeyAsync(connectionId, sourceEntity, cancellationToken);
        var products = (await _deltaReader.ReadProductsDeltaAsync(connectionId, lastKey, cancellationToken)).ToList();

        int processed = 0;
        string? newLastKey = lastKey;
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

                // Use a dedicated ExternalSystem for products; upgrade legacy records that used "Component2020".
                Item? existing = null;

                if (existingLinksByExternalId.TryGetValue(externalId, out var existingLink))
                {
                    existingItemsById.TryGetValue(existingLink.EntityId, out existing);
                }

                if (existing == null)
                {
                    existing = await _dbContext.Items
                        .FirstOrDefaultAsync(i => i.ExternalSystem == externalSystem && i.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    existing = await _dbContext.Items
                        .FirstOrDefaultAsync(i => i.ExternalSystem == legacyExternalSystem && i.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var defaultUoM = await FindDefaultUnitOfMeasureAsync(cancellationToken);
                        var newItem = new Item(product.PartNumber ?? product.Id.ToString(), product.Name, ItemKind.Product, defaultUoM.Id);
                        var now = DateTimeOffset.UtcNow;
                        newItem.SetExternalReference(externalSystem, externalId, now);
                        _dbContext.Items.Add(newItem);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, newItem.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }
                else
                {
                    if (!dryRun)
                    {
                        existing.Update(product.Name, existing.UnitOfMeasureId, existing.ItemGroupId, existing.IsEskd, existing.IsEskdDocument, existing.ManufacturerPartNumber);
                        var now = DateTimeOffset.UtcNow;
                        existing.SetExternalReference(externalSystem, externalId, now);
                        EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
                    }
                    processed++;
                }

                newLastKey = Math.Max(int.Parse(newLastKey ?? "0"), product.Id).ToString();
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
                    existing = await _dbContext.Manufacturers
                        .FirstOrDefaultAsync(m => m.ExternalSystem == externalSystem && m.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        // Access Manufact does not have a code field; keep Code = null (user-managed).
                        var now = DateTimeOffset.UtcNow;
                        var created = new Manufacturer(null, manufacturer.Name, manufacturer.FullName, manufacturer.Site, manufacturer.Note, externalSystem, externalId);
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
                        existing.MarkSynced();
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
                    existing = await _dbContext.BodyTypes
                        .FirstOrDefaultAsync(bt => bt.ExternalSystem == externalSystem && bt.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new BodyType(bodyType.Name, bodyType.Name, bodyType.Description, bodyType.Pins, bodyType.Smt, bodyType.Photo, bodyType.FootPrintPath, bodyType.FootprintRef, bodyType.FootprintRef2, bodyType.FootPrintRef3, externalSystem, externalId);
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
                        existing.MarkSynced();
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
                    existing = await _dbContext.Currencies
                        .FirstOrDefaultAsync(c => c.ExternalSystem == externalSystem && c.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new Currency(code, currency.Name, symbol, currency.Rate, externalSystem, externalId);
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
                        existing.MarkSynced();
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
                    existing = await _dbContext.TechnicalParameters
                        .FirstOrDefaultAsync(tp => tp.ExternalSystem == externalSystem && tp.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new TechnicalParameter(technicalParameter.Name, technicalParameter.Name, technicalParameter.Symbol, technicalParameter.UnitId, externalSystem, externalId);
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
                        existing.MarkSynced();
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
                    existing = await _dbContext.ParameterSets
                        .FirstOrDefaultAsync(ps => ps.ExternalSystem == externalSystem && ps.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new ParameterSet(parameterSet.Name, parameterSet.Name, parameterSet.P0Id, parameterSet.P1Id, parameterSet.P2Id, parameterSet.P3Id, parameterSet.P4Id, parameterSet.P5Id, externalSystem, externalId);
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
                        existing.MarkSynced();
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

                // Backward-compatibility: try legacy fields (ExternalSystem/ExternalId) to link older data.
                if (existing == null)
                {
                    existing = await _dbContext.Symbols
                        .FirstOrDefaultAsync(s => s.ExternalSystem == externalSystem && s.ExternalId == externalId, cancellationToken);
                }

                if (existing == null)
                {
                    if (!dryRun)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var created = new Symbol(symbol.Name, symbol.Name, symbol.SymbolValue, symbol.Photo, symbol.LibraryPath, symbol.LibraryRef, externalSystem, externalId);
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
                        existing.MarkSynced();
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

    private async Task<int> DeleteMissingUnitsAsync(HashSet<string> incomingExternalIds, Guid runId, List<Component2020SyncError> errors, CancellationToken cancellationToken)
    {
        const string entityType = "UnitOfMeasure";
        const string linkEntityType = nameof(UnitOfMeasure);

        var candidates = await _dbContext.UnitOfMeasures
            .Where(u =>
                u.ExternalSystem == "Component2020"
                && u.ExternalId != null
                && !incomingExternalIds.Contains(u.ExternalId))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var candidateIds = candidates.Select(u => u.Id).ToHashSet();

        var referencedByItems = await _dbContext.Items
            .Where(i => candidateIds.Contains(i.UnitOfMeasureId))
            .Select(i => i.UnitOfMeasureId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referencedByLines = await _dbContext.RequestLines
            .Where(l => l.UnitOfMeasureId != null && candidateIds.Contains(l.UnitOfMeasureId.Value))
            .Select(l => l.UnitOfMeasureId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var referenced = referencedByItems.Concat(referencedByLines).ToHashSet();

        var blocked = candidates.Where(u => referenced.Contains(u.Id)).ToList();
        foreach (var unit in blocked)
        {
            errors.Add(new Component2020SyncError(
                runId,
                entityType,
                null,
                unit.ExternalId ?? unit.Id.ToString(),
                "Cannot delete unit because it is referenced by existing documents/items.",
                null));
        }

        var toDelete = candidates.Where(u => !referenced.Contains(u.Id)).ToList();
        if (toDelete.Count == 0)
        {
            return 0;
        }

        var toDeleteIds = toDelete.Select(u => u.Id).ToList();
        var linksToDelete = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == linkEntityType && toDeleteIds.Contains(l.EntityId))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(linksToDelete);
        _dbContext.UnitOfMeasures.RemoveRange(toDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return toDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for UnitOfMeasure overwrite cleanup, falling back to per-row deletes");
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var unit in toDelete)
            {
                try
                {
                    var current = await _dbContext.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == unit.Id, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    var currentLinks = await _dbContext.ExternalEntityLinks
                        .Where(l => l.EntityType == linkEntityType && l.EntityId == current.Id)
                        .ToListAsync(cancellationToken);
                    _dbContext.ExternalEntityLinks.RemoveRange(currentLinks);
                    _dbContext.UnitOfMeasures.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete UnitOfMeasure {UnitId} during overwrite cleanup", unit.Id);
                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        unit.ExternalId ?? unit.Id.ToString(),
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
        var legacyMissingKeys = await set
            .Where(e =>
                EF.Property<string?>(e, "ExternalSystem") == externalSystem
                && EF.Property<string?>(e, "ExternalId") != null
                && !incomingExternalIds.Contains(EF.Property<string?>(e, "ExternalId")!))
            .Select(e => new { Id = EF.Property<Guid>(e, "Id"), ExternalId = EF.Property<string?>(e, "ExternalId")! })
            .ToListAsync(cancellationToken);

        var missingLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.EntityId, l.ExternalId })
            .ToListAsync(cancellationToken);

        if (missingLinks.Count == 0 && legacyMissingKeys.Count == 0)
        {
            return 0;
        }

        var externalIdByEntityId = new Dictionary<Guid, string>();
        foreach (var key in legacyMissingKeys)
        {
            externalIdByEntityId[key.Id] = key.ExternalId;
        }

        foreach (var link in missingLinks)
        {
            if (!externalIdByEntityId.ContainsKey(link.EntityId))
            {
                externalIdByEntityId[link.EntityId] = link.ExternalId;
            }
        }

        var affectedEntityIds = missingLinks.Select(x => x.EntityId).Concat(legacyMissingKeys.Select(x => x.Id)).Distinct().ToList();
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

        // Ensure backward-compatibility: also remove entities that were imported before external links existed.
        var entityIdsToDelete = legacyMissingKeys.Select(x => x.Id).ToHashSet();
        foreach (var id in entityIdsToDeleteByLinks)
        {
            entityIdsToDelete.Add(id);
        }

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
                            var deactivate = current.GetType().GetMethod("Deactivate", Type.EmptyTypes);
                            if (deactivate != null)
                            {
                                deactivate.Invoke(current, null);
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

    private async Task<int> DeleteMissingByExternalKeyAsync<TEntity>(
        DbSet<TEntity> set,
        string externalSystem,
        HashSet<string> incomingExternalIds,
        Guid runId,
        string entityType,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var missingKeys = await set
            .Where(e =>
                EF.Property<string?>(e, "ExternalSystem") == externalSystem
                && EF.Property<string?>(e, "ExternalId") != null
                && !incomingExternalIds.Contains(EF.Property<string?>(e, "ExternalId")!))
            .Select(e => new
            {
                Id = EF.Property<Guid>(e, "Id"),
                ExternalId = EF.Property<string?>(e, "ExternalId")
            })
            .ToListAsync(cancellationToken);

        if (missingKeys.Count == 0)
        {
            return 0;
        }

        var ids = missingKeys.Select(x => x.Id).ToList();
        var toDelete = await set.Where(e => ids.Contains(EF.Property<Guid>(e, "Id"))).ToListAsync(cancellationToken);
        set.RemoveRange(toDelete);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return toDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bulk delete failed for {EntityType} overwrite cleanup, falling back to per-row deletes", entityType);
            _dbContext.ChangeTracker.Clear();

            var deleted = 0;
            foreach (var key in missingKeys)
            {
                try
                {
                    var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == key.Id, cancellationToken);
                    if (current == null)
                    {
                        continue;
                    }

                    set.Remove(current);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    deleted++;
                }
                catch (Exception rowEx)
                {
                    _logger.LogWarning(rowEx, "Failed to delete {EntityType} {ExternalId} during overwrite cleanup", entityType, key.ExternalId ?? key.Id.ToString());

                    var message = "Cannot delete record during overwrite cleanup.";
                    try
                    {
                        var current = await set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == key.Id, cancellationToken);
                        if (current != null)
                        {
                            var deactivate = current.GetType().GetMethod("Deactivate", Type.EmptyTypes);
                            if (deactivate != null)
                            {
                                deactivate.Invoke(current, null);
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

                    errors.Add(new Component2020SyncError(
                        runId,
                        entityType,
                        null,
                        key.ExternalId ?? key.Id.ToString(),
                        message,
                        rowEx.ToString()));
                }
            }

            return deleted;
        }
    }
}
