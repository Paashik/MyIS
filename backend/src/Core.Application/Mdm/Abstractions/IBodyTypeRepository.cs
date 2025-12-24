using System;
using System.Threading.Tasks;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Application.Mdm.Abstractions;

public interface IBodyTypeRepository
{
    Task<BodyType?> FindByIdAsync(Guid id);
    Task<BodyType?> FindByCodeAsync(string code);
    Task<bool> ExistsByCodeAsync(string code);
    Task AddAsync(BodyType bodyType);
    Task UpdateAsync(BodyType bodyType);
}
