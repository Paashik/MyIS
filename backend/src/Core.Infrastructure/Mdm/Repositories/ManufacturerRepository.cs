using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Mdm.Repositories;

public sealed class ManufacturerRepository : IManufacturerRepository
{
    private readonly AppDbContext _dbContext;

    public ManufacturerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Manufacturer?> FindByIdAsync(Guid id)
    {
        return await _dbContext.Manufacturers
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddAsync(Manufacturer manufacturer)
    {
        if (manufacturer is null) throw new ArgumentNullException(nameof(manufacturer));

        await _dbContext.Manufacturers.AddAsync(manufacturer);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Manufacturer manufacturer)
    {
        if (manufacturer is null) throw new ArgumentNullException(nameof(manufacturer));

        _dbContext.Manufacturers.Update(manufacturer);
        await _dbContext.SaveChangesAsync();
    }
}
