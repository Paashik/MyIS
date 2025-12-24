using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IParameterSetRepository
{
    Task<ParameterSet?> FindByIdAsync(Guid id);
    Task<ParameterSet?> FindByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(ParameterSet parameterSet);
    Task UpdateAsync(ParameterSet parameterSet);
}
