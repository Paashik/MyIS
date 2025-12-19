using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface ICurrencyRepository
{
    Task<Currency?> FindByIdAsync(Guid id);
    Task<Currency?> FindByCodeAsync(string code);
    Task<Currency?> FindByExternalIdAsync(string externalSystem, string externalId);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(Currency currency);
    Task UpdateAsync(Currency currency);
}