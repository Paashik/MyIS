using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Customers.Entities;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020CustomerOrdersSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020CustomerOrdersSyncHandler> _logger;

    public Component2020CustomerOrdersSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020CustomerOrdersSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.CustomerOrders;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
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
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
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
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, DateTimeOffset.UtcNow);
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
            var deleted = await _externalLinkHelper.DeleteMissingByExternalLinkAsync(
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
}

