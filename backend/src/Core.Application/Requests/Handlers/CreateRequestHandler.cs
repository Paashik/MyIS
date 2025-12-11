using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class CreateRequestHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public CreateRequestHandler(
        IRequestRepository requestRepository,
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<RequestDto> Handle(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (command.InitiatorId == Guid.Empty)
        {
            throw new ArgumentException("InitiatorId is required.", nameof(command));
        }

        if (command.RequestTypeId == Guid.Empty)
        {
            throw new ArgumentException("RequestTypeId is required.", nameof(command));
        }

        // 1. Загрузка типа заявки
        var typeId = new RequestTypeId(command.RequestTypeId);
        var requestType = await _requestTypeRepository.GetByIdAsync(typeId, cancellationToken);
        if (requestType is null)
        {
            throw new InvalidOperationException($"RequestType with id '{command.RequestTypeId}' was not found.");
        }

        // 2. Загрузка стартового статуса (Draft)
        var draftStatus = await _requestStatusRepository.GetByCodeAsync(RequestStatusCode.Draft, cancellationToken);
        if (draftStatus is null)
        {
            throw new InvalidOperationException("Initial RequestStatus 'Draft' is not configured.");
        }

        // 3. Проверка прав
        await _accessChecker.EnsureCanCreateAsync(command.InitiatorId, requestType, cancellationToken);

        // 4. Создание доменной сущности
        var now = DateTimeOffset.UtcNow;

        var request = Request.Create(
            requestType,
            draftStatus,
            command.InitiatorId,
            command.Title,
            command.Description,
            now,
            command.DueDate,
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.ExternalReferenceId);

        // 5. Сохранение в репозиторий
        await _requestRepository.AddAsync(request, cancellationToken);

        // 6. Маппинг в DTO
        var dto = MapToDto(request, requestType, draftStatus, initiatorFullName: null);

        return dto;
    }

    private static RequestDto MapToDto(
        Request request,
        RequestType requestType,
        RequestStatus status,
        string? initiatorFullName)
    {
        return new RequestDto
        {
            Id = request.Id.Value,
            Title = request.Title,
            Description = request.Description,
            RequestTypeId = requestType.Id.Value,
            RequestTypeCode = requestType.Code,
            RequestTypeName = requestType.Name,
            RequestStatusId = status.Id.Value,
            RequestStatusCode = status.Code.Value,
            RequestStatusName = status.Name,
            InitiatorId = request.InitiatorId,
            InitiatorFullName = initiatorFullName,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            ExternalReferenceId = request.ExternalReferenceId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            DueDate = request.DueDate
        };
    }
}