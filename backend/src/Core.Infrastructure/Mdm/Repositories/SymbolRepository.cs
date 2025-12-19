using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class SymbolRepository : ISymbolRepository
{
    private readonly AppDbContext _dbContext;

    public SymbolRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Symbol?> FindByIdAsync(Guid id)
    {
        return await _dbContext.Symbols
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Symbol?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.Symbols
            .FirstOrDefaultAsync(s => s.Code == normalized);
    }

    public async Task<Symbol?> FindByExternalIdAsync(string externalSystem, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        }

        return await _dbContext.Symbols
            .FirstOrDefaultAsync(s => s.ExternalSystem == externalSystem && s.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.Symbols
            .AnyAsync(s => s.Code == normalized);
    }

    public async Task AddAsync(Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        await _dbContext.Symbols.AddAsync(symbol);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Symbol symbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        _dbContext.Symbols.Update(symbol);
        await _dbContext.SaveChangesAsync();
    }
}