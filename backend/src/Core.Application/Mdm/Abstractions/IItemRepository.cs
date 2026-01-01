using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IItemRepository
{
    Task<Item?> FindByIdAsync(Guid id);
    Task<Item?> FindByCodeAsync(string code);
    Task<Item?> FindByExternalAsync(string externalSystem, string externalId);
    
    /// <summary>
    /// Находит Items по списку внешних идентификаторов за один запрос
    /// </summary>
    /// <param name="externalSystem">Название внешней системы (например, "Component2020")</param>
    /// <param name="externalIds">Список внешних идентификаторов</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Словарь: ключ - внешний ID, значение - Item</returns>
    Task<Dictionary<string, Item>> FindByExternalBatchAsync(
        string externalSystem,
        List<string> externalIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Item>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(Guid id);
}