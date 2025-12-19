using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IItemAttributeRepository
{
    Task<ItemAttribute?> FindByIdAsync(Guid id);
    Task<ItemAttribute?> FindByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(ItemAttribute attribute);
    Task UpdateAsync(ItemAttribute attribute);
}