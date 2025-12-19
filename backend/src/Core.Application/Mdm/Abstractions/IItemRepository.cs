using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IItemRepository
{
    Task<Item?> FindByIdAsync(Guid id);
    Task<Item?> FindByCodeAsync(string code);
    Task<Item?> FindByExternalAsync(string externalSystem, string externalId);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(Guid id);
}