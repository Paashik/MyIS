using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _dbContext;

    public CurrencyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Currency?> FindByIdAsync(Guid id)
    {
        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Currency?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.Code == normalized);
    }

    public async Task<Currency?> FindByExternalIdAsync(string externalSystem, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        }

        return await _dbContext.Currencies
            .FirstOrDefaultAsync(c => c.ExternalSystem == externalSystem && c.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.Currencies
            .AnyAsync(c => c.Code == normalized);
    }

    public async Task AddAsync(Currency currency)
    {
        if (currency is null) throw new ArgumentNullException(nameof(currency));

        await _dbContext.Currencies.AddAsync(currency);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Currency currency)
    {
        if (currency is null) throw new ArgumentNullException(nameof(currency));

        _dbContext.Currencies.Update(currency);
        await _dbContext.SaveChangesAsync();
    }
}