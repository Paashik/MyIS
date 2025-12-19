using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IUnitOfMeasureRepository
{
    Task<UnitOfMeasure?> FindByIdAsync(Guid id);
    Task<UnitOfMeasure?> FindByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(UnitOfMeasure unit);
    Task UpdateAsync(UnitOfMeasure unit);
}