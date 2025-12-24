using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IItemGroupRepository
{
    Task<ItemGroup?> FindByIdAsync(Guid id);
    Task AddAsync(ItemGroup group);
    Task UpdateAsync(ItemGroup group);
}
