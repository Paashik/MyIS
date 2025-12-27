using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020EmployeesSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020EmployeesSyncHandler> _logger;

    public Component2020EmployeesSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020EmployeesSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Employees;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
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
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
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

                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
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
            var deleted = await _externalLinkHelper.DeleteMissingByExternalLinkAsync(
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

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string BuildPersonFullName(Component2020Person person)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(person.LastName)) parts.Add(person.LastName.Trim());
        if (!string.IsNullOrWhiteSpace(person.FirstName)) parts.Add(person.FirstName.Trim());
        if (!string.IsNullOrWhiteSpace(person.SecondName)) parts.Add(person.SecondName.Trim());

        return string.Join(" ", parts);
    }
}

