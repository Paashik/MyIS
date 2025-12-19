using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class ParameterSetRepository : IParameterSetRepository
{
    private readonly AppDbContext _dbContext;

    public ParameterSetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<ParameterSet?> FindByIdAsync(Guid id)
    {
        return await _dbContext.ParameterSets
            .FirstOrDefaultAsync(ps => ps.Id == id);
    }

    public async Task<ParameterSet?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.ParameterSets
            .FirstOrDefaultAsync(ps => ps.Code == normalized);
    }

    public async Task<ParameterSet?> FindByExternalIdAsync(string externalSystem, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        }

        return await _dbContext.ParameterSets
            .FirstOrDefaultAsync(ps => ps.ExternalSystem == externalSystem && ps.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.ParameterSets
            .AnyAsync(ps => ps.Code == normalized);
    }

    public async Task AddAsync(ParameterSet parameterSet)
    {
        if (parameterSet is null) throw new ArgumentNullException(nameof(parameterSet));

        await _dbContext.ParameterSets.AddAsync(parameterSet);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(ParameterSet parameterSet)
    {
        if (parameterSet is null) throw new ArgumentNullException(nameof(parameterSet));

        _dbContext.ParameterSets.Update(parameterSet);
        await _dbContext.SaveChangesAsync();
    }
}