using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Domain.Organization;
using MyIS.Core.Domain.Users;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services.Sync;

public sealed class Component2020UsersSyncHandler : IComponent2020SyncHandler
{
    private readonly AppDbContext _dbContext;
    private readonly IComponent2020DeltaReader _deltaReader;
    private readonly IComponent2020SyncCursorRepository _cursorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Component2020ExternalLinkHelper _externalLinkHelper;
    private readonly ILogger<Component2020UsersSyncHandler> _logger;

    public Component2020UsersSyncHandler(
        AppDbContext dbContext,
        IComponent2020DeltaReader deltaReader,
        IComponent2020SyncCursorRepository cursorRepository,
        IPasswordHasher passwordHasher,
        Component2020ExternalLinkHelper externalLinkHelper,
        ILogger<Component2020UsersSyncHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _deltaReader = deltaReader ?? throw new ArgumentNullException(nameof(deltaReader));
        _cursorRepository = cursorRepository ?? throw new ArgumentNullException(nameof(cursorRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _externalLinkHelper = externalLinkHelper ?? throw new ArgumentNullException(nameof(externalLinkHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Component2020SyncScope Scope => Component2020SyncScope.Users;

    public async Task<(int processed, List<Component2020SyncError> errors)> SyncAsync(
        Guid connectionId,
        bool dryRun,
        Component2020SyncMode syncMode,
        Guid runId,
        Dictionary<string, int> counters,
        List<Component2020SyncError> errors,
        CancellationToken cancellationToken)
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
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, created.Id, externalSystem, externalEntity, externalId, null, now);
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
                        _externalLinkHelper.EnsureExternalEntityLink(existingLinksByExternalId, linkEntityType, existing.Id, externalSystem, externalEntity, externalId, null, now);
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
            var deleted = await _externalLinkHelper.DeleteMissingByExternalLinkAsync(
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

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

