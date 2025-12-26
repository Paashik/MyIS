using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class DeleteRequestHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public DeleteRequestHandler(
        IRequestRepository requestRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task Handle(DeleteRequestCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.Id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(command));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(command));
        }

        // 1. Загрузка заявки
        var requestId = new RequestId(command.Id);
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{command.Id}' was not found.");
        }

        // 2. Проверка прав на удаление (только инициатор или администратор)
        await _accessChecker.EnsureCanDeleteAsync(command.CurrentUserId, request, cancellationToken);

        // 3. Удаление
        await _requestRepository.DeleteAsync(requestId, cancellationToken);
    }
}