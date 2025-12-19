using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class BodyTypeRepository : IBodyTypeRepository
{
    private readonly AppDbContext _dbContext;

    public BodyTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<BodyType?> FindByIdAsync(Guid id)
    {
        return await _dbContext.BodyTypes
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<BodyType?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var normalized = code.Trim();

        return await _dbContext.BodyTypes
            .FirstOrDefaultAsync(b => b.Code == normalized);
    }

    public async Task<BodyType?> FindByExternalIdAsync(string externalSystem, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalSystem))
        {
            throw new ArgumentException("ExternalSystem is required.", nameof(externalSystem));
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new ArgumentException("ExternalId is required.", nameof(externalId));
        }

        return await _dbContext.BodyTypes
            .FirstOrDefaultAsync(b => b.ExternalSystem == externalSystem && b.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();

        return await _dbContext.BodyTypes
            .AnyAsync(b => b.Code == normalized);
    }

    public async Task AddAsync(BodyType bodyType)
    {
        if (bodyType is null) throw new ArgumentNullException(nameof(bodyType));

        await _dbContext.BodyTypes.AddAsync(bodyType);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(BodyType bodyType)
    {
        if (bodyType is null) throw new ArgumentNullException(nameof(bodyType));

        _dbContext.BodyTypes.Update(bodyType);
        await _dbContext.SaveChangesAsync();
    }
}