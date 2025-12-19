using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface ITechnicalParameterRepository
{
    Task<TechnicalParameter?> FindByIdAsync(Guid id);
    Task<TechnicalParameter?> FindByCodeAsync(string code);
    Task<TechnicalParameter?> FindByExternalIdAsync(string externalSystem, string externalId);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(TechnicalParameter technicalParameter);
    Task UpdateAsync(TechnicalParameter technicalParameter);
}