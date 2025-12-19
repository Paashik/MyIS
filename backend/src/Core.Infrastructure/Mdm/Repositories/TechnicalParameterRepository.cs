using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class TechnicalParameterRepository : ITechnicalParameterRepository
{
    private readonly AppDbContext _dbContext;

    public TechnicalParameterRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<TechnicalParameter?> FindByIdAsync(Guid id)
    {
        return await _dbContext.TechnicalParameters
            .FirstOrDefaultAsync(tp => tp.Id == id);
    }

    public async Task<TechnicalParameter?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.TechnicalParameters
            .FirstOrDefaultAsync(tp => tp.Code == normalized);
    }

    public async Task<TechnicalParameter?> FindByExternalIdAsync(string externalSystem, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        }

        return await _dbContext.TechnicalParameters
            .FirstOrDefaultAsync(tp => tp.ExternalSystem == externalSystem && tp.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.TechnicalParameters
            .AnyAsync(tp => tp.Code == normalized);
    }

    public async Task AddAsync(TechnicalParameter technicalParameter)
    {
        if (technicalParameter is null) throw new ArgumentNullException(nameof(technicalParameter));

        await _dbContext.TechnicalParameters.AddAsync(technicalParameter);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(TechnicalParameter technicalParameter)
    {
        if (technicalParameter is null) throw new ArgumentNullException(nameof(technicalParameter));

        _dbContext.TechnicalParameters.Update(technicalParameter);
        await _dbContext.SaveChangesAsync();
    }
}