using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;

namespace MyIS.Core.Application.Integration.Component2020.Handlers;

public class ScheduleComponent2020SyncHandler
{
    private readonly IComponent2020SyncScheduleRepository _scheduleRepository;

    public ScheduleComponent2020SyncHandler(IComponent2020SyncScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
    }

    public async Task<ScheduleComponent2020SyncResponse> Handle(ScheduleComponent2020SyncCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var scheduleDto = new Component2020SyncScheduleDto
        {
            Name = $"Sync {command.Scope}",
            CronExpression = command.CronExpression,
            Scope = command.Scope,
            IsActive = command.IsActive
        };

        await _scheduleRepository.AddAsync(scheduleDto, cancellationToken);

        return new ScheduleComponent2020SyncResponse
        {
            ScheduleId = Guid.NewGuid(), // TODO: return actual id
            Status = "Scheduled"
        };
    }
}