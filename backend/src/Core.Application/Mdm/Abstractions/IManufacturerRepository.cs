using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IManufacturerRepository
{
    Task<Manufacturer?> FindByIdAsync(Guid id);
    Task AddAsync(Manufacturer manufacturer);
    Task UpdateAsync(Manufacturer manufacturer);
}
