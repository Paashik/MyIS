using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020CounterpartiesSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020CounterpartiesSyncHandler> _logger;

    public Component2020CounterpartiesSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020CounterpartiesSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Counterparties;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
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
                        counterparty = await FindExistingCounterpartyAsync(inn, kpp, name, fullName, cancellationToken);
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

                    _externalLinkHelper.EnsureExternalEntityLink(
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
            var (linksDeleted, counterpartiesDeleted) = await DeleteMissingCounterpartiesAsync(
                linkEntityType,
                externalSystem,
                externalEntity,
                incomingExternalIds,
                cancellationToken);
            counters["ProviderLinkDeleted"] = linksDeleted;
            counters["CounterpartyDeleted"] = counterpartiesDeleted;
        }

        counters[entityType] = processed;

        _logger.LogInformation(
            "Component2020 Providers sync finished: processed={Processed}, errors={Errors}, elapsedMs={ElapsedMs}",
            processed,
            errors.Count,
            sw.ElapsedMilliseconds);

        return (processed, errors);
    }

    private async Task<(int linksDeleted, int counterpartiesDeleted)> DeleteMissingCounterpartiesAsync(
        string linkEntityType,
        string externalSystem,
        string externalEntity,
        HashSet<string> incomingExternalIds,
        CancellationToken cancellationToken)
    {
        var missingLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && l.ExternalSystem == externalSystem
                && l.ExternalEntity == externalEntity
                && !incomingExternalIds.Contains(l.ExternalId))
            .Select(l => new { l.Id, l.EntityId })
            .ToListAsync(cancellationToken);

        var candidateCounterpartyIds = missingLinks
            .Select(l => l.EntityId)
            .Distinct()
            .ToList();

        var orphanIds = await _dbContext.Counterparties
            .Where(c => !_dbContext.ExternalEntityLinks.Any(l => l.EntityType == linkEntityType && l.EntityId == c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (missingLinks.Count == 0 && orphanIds.Count == 0)
        {
            LogCleanup("no missing links and no orphans (incoming={IncomingCount})", incomingExternalIds.Count);
            return (0, 0);
        }

        var protectedIds = new List<Guid>();
        if (candidateCounterpartyIds.Count > 0)
        {
            var protectedByOrders = await _dbContext.CustomerOrders
                .Where(o => o.CustomerId.HasValue && candidateCounterpartyIds.Contains(o.CustomerId.Value))
                .Select(o => o.CustomerId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            protectedIds = protectedIds.Concat(protectedByOrders).ToList();

            var protectedByRequests = await _dbContext.Requests
                .Where(r =>
                    ((r.TargetEntityType == "Counterparty" && r.TargetEntityId.HasValue)
                     || (r.RelatedEntityType == "Counterparty" && r.RelatedEntityId.HasValue))
                    && (candidateCounterpartyIds.Contains(r.TargetEntityId ?? Guid.Empty)
                        || candidateCounterpartyIds.Contains(r.RelatedEntityId ?? Guid.Empty)))
                .Select(r => r.TargetEntityType == "Counterparty" ? r.TargetEntityId : r.RelatedEntityId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (protectedByRequests.Count > 0)
            {
                protectedIds = protectedIds.Concat(protectedByRequests).ToList();
            }
        }

        if (orphanIds.Count > 0)
        {
            var protectedOrphansByOrders = await _dbContext.CustomerOrders
                .Where(o => o.CustomerId.HasValue && orphanIds.Contains(o.CustomerId.Value))
                .Select(o => o.CustomerId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (protectedOrphansByOrders.Count > 0)
            {
                protectedIds = protectedIds.Concat(protectedOrphansByOrders).ToList();
            }

            var protectedOrphansByRequests = await _dbContext.Requests
                .Where(r =>
                    ((r.TargetEntityType == "Counterparty" && r.TargetEntityId.HasValue)
                     || (r.RelatedEntityType == "Counterparty" && r.RelatedEntityId.HasValue))
                    && (orphanIds.Contains(r.TargetEntityId ?? Guid.Empty)
                        || orphanIds.Contains(r.RelatedEntityId ?? Guid.Empty)))
                .Select(r => r.TargetEntityType == "Counterparty" ? r.TargetEntityId : r.RelatedEntityId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (protectedOrphansByRequests.Count > 0)
            {
                protectedIds = protectedIds.Concat(protectedOrphansByRequests).ToList();
            }
        }

        protectedIds = protectedIds.Distinct().ToList();

        var deletableIds = candidateCounterpartyIds
            .Concat(orphanIds)
            .Distinct()
            .Where(id => !protectedIds.Contains(id))
            .ToList();

        if (deletableIds.Count == 0)
        {
            LogCleanup(
                "candidates={Candidates}, orphans={Orphans}, protected={Protected} -> nothing to delete",
                candidateCounterpartyIds.Count,
                orphanIds.Count,
                protectedIds.Count);
            return (0, 0);
        }

        var linkIdsToDelete = missingLinks
            .Where(l => deletableIds.Contains(l.EntityId))
            .Select(l => l.Id)
            .ToList();

        if (linkIdsToDelete.Count > 0)
        {
            var links = await _dbContext.ExternalEntityLinks
                .Where(l => linkIdsToDelete.Contains(l.Id))
                .ToListAsync(cancellationToken);
            _dbContext.ExternalEntityLinks.RemoveRange(links);
        }

        var otherLinks = await _dbContext.ExternalEntityLinks
            .Where(l =>
                l.EntityType == linkEntityType
                && deletableIds.Contains(l.EntityId)
                && !linkIdsToDelete.Contains(l.Id))
            .ToListAsync(cancellationToken);

        _dbContext.ExternalEntityLinks.RemoveRange(otherLinks);

        var counterparties = await _dbContext.Counterparties
            .Where(c => deletableIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        _dbContext.Counterparties.RemoveRange(counterparties);

        await _dbContext.SaveChangesAsync(cancellationToken);
        LogCleanup(
            "candidates={Candidates}, orphans={Orphans}, protected={Protected}, linksDeleted={LinksDeleted}, counterpartiesDeleted={CounterpartiesDeleted}",
            candidateCounterpartyIds.Count,
            orphanIds.Count,
            protectedIds.Count,
            linkIdsToDelete.Count + otherLinks.Count,
            counterparties.Count);
        return (linkIdsToDelete.Count + otherLinks.Count, counterparties.Count);
    }

    private void LogCleanup(string message, params object[] args)
    {
        var formatted = $"[C2020-CLEANUP] {message}";
        _logger.LogWarning(formatted, args);
        try
        {
            Console.WriteLine(string.Format(formatted, args));
        }
        catch
        {
            Console.WriteLine(formatted);
        }
    }

    private async Task<Counterparty?> FindExistingCounterpartyAsync(
        string? inn,
        string? kpp,
        string name,
        string? fullName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(inn))
        {
            var normalizedKpp = kpp ?? string.Empty;
            return await _dbContext.Counterparties
                .FirstOrDefaultAsync(
                    x => x.Inn == inn && (x.Kpp ?? string.Empty) == normalizedKpp,
                    cancellationToken);
        }

        var byName = await _dbContext.Counterparties
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (byName != null)
        {
            return byName;
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return await _dbContext.Counterparties
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync(x => x.FullName == fullName, cancellationToken);
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

