using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Queries;

namespace MyIS.Core.Application.Integration.Component2020.Abstractions;

public interface IComponent2020SyncScheduleRepository
{
    Task AddAsync(Component2020SyncScheduleDto schedule, CancellationToken cancellationToken);
    Task<Component2020SyncScheduleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Component2020SyncScheduleDto schedule, CancellationToken cancellationToken);
    Task<bool> HasActiveSchedulesAsync(CancellationToken cancellationToken);
}