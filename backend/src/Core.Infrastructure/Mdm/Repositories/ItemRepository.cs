using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class ItemRepository : IItemRepository
{
    private readonly AppDbContext _dbContext;

    public ItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Item?> FindByIdAsync(Guid id)
    {
        return await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item?> FindByCodeAsync(string code)
    {
        return await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Code == code);
    }

    public async Task<Item?> FindByExternalAsync(string externalSystem, string externalId)
    {
        var link = await _dbContext.ExternalEntityLinks
            .FirstOrDefaultAsync(l => l.EntityType == "Item" &&
                                     l.ExternalSystem == externalSystem &&
                                     l.ExternalId == externalId);

        if (link == null)
        {
            return null;
        }

        return await FindByIdAsync(link.EntityId);
    }

    public async Task<Dictionary<string, Item>> FindByExternalBatchAsync(
        string externalSystem,
        List<string> externalIds,
        CancellationToken cancellationToken = default)
    {
        if (externalIds == null || externalIds.Count == 0)
        {
            return new Dictionary<string, Item>();
        }

        // Загружаем все связи за один запрос
        var links = await _dbContext.ExternalEntityLinks
            .Where(l => l.EntityType == "Item" &&
                       l.ExternalSystem == externalSystem &&
                       externalIds.Contains(l.ExternalId))
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
        {
            return new Dictionary<string, Item>();
        }

        // Загружаем все Items за один запрос
        var entityIds = links.Select(l => l.EntityId).ToList();
        var items = await _dbContext.Items
            .Where(i => entityIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        // Создаем словарь для быстрого поиска Item по EntityId
        var itemsById = items.ToDictionary(i => i.Id);

        // Формируем результат: ExternalId -> Item
        var result = new Dictionary<string, Item>();
        foreach (var link in links)
        {
            if (itemsById.TryGetValue(link.EntityId, out var item))
            {
                result[link.ExternalId] = item;
            }
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, Item>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids is null) throw new ArgumentNullException(nameof(ids));
        if (ids.Count == 0) return new Dictionary<Guid, Item>();

        var items = await _dbContext.Items
            .AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);

        return items.ToDictionary(x => x.Id, x => x);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        return await _dbContext.Items
            .AnyAsync(i => i.Code == code);
    }

    public async Task AddAsync(Item item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        await _dbContext.Items.AddAsync(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Item item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        _dbContext.Items.Update(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await FindByIdAsync(id);
        if (item != null)
        {
            _dbContext.Items.Remove(item);
            await _dbContext.SaveChangesAsync();
        }
    }
}