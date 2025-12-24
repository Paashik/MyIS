using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface ISymbolRepository
{
    Task<Symbol?> FindByIdAsync(Guid id);
    Task<Symbol?> FindByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(Symbol symbol);
    Task UpdateAsync(Symbol symbol);
}
