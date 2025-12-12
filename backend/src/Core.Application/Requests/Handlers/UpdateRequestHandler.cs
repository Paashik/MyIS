using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class UpdateRequestHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public UpdateRequestHandler(
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

    public async Task<RequestDto> Handle(UpdateRequestCommand command, CancellationToken cancellationToken)
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

        // 2. Загрузка текущего типа и статуса
        var requestType = await _requestTypeRepository.GetByIdAsync(request.RequestTypeId, cancellationToken);
        if (requestType is null)
        {
            throw new InvalidOperationException($"RequestType with id '{request.RequestTypeId.Value}' was not found.");
        }

        var status = await _requestStatusRepository.GetByIdAsync(request.RequestStatusId, cancellationToken);
        if (status is null)
        {
            throw new InvalidOperationException($"RequestStatus with id '{request.RequestStatusId.Value}' was not found.");
        }

        // 3. Проверка прав на редактирование
        await _accessChecker.EnsureCanUpdateAsync(command.CurrentUserId, request, cancellationToken);

        // 4. Обновление деталей (нельзя, если статус финальный)
        var now = DateTimeOffset.UtcNow;

        request.UpdateDetails(
            command.Title,
            command.Description,
            command.DueDate,
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.ExternalReferenceId,
            now,
            isCurrentStatusFinal: status.IsFinal);

        // 4.1. Позиционное тело (v0.1: replace-all)
        if (command.Lines is not null)
        {
            await _accessChecker.EnsureCanEditBodyAsync(command.CurrentUserId, request, cancellationToken);
            var lines = MapToDomainLines(request.Id, command.Lines);
            request.ReplaceLines(lines, now, isCurrentStatusFinal: status.IsFinal);
        }

        // 5. Сохранение
        await _requestRepository.UpdateAsync(request, cancellationToken);

        // 6. Маппинг в DTO
        var dto = MapToDto(request, requestType, status, initiatorFullName: null);

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
            BodyText = request.Description,
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
            DueDate = request.DueDate,
            Lines = MapToLineDtos(request)
        };
    }

    private static RequestLine[] MapToDomainLines(RequestId requestId, RequestLineInputDto[] lines)
    {
        var result = new RequestLine[lines.Length];
        for (var i = 0; i < lines.Length; i++)
        {
            var l = lines[i];
            result[i] = RequestLine.Create(
                requestId,
                l.LineNo,
                l.ItemId,
                l.ExternalItemCode,
                l.Description,
                l.Quantity,
                l.UnitOfMeasureId,
                l.NeedByDate,
                l.SupplierName,
                l.SupplierContact,
                l.ExternalRowReferenceId);
        }

        return result;
    }

    private static RequestLineDto[] MapToLineDtos(Request request)
    {
        if (request.Lines.Count == 0) return Array.Empty<RequestLineDto>();

        var result = new RequestLineDto[request.Lines.Count];
        var i = 0;
        foreach (var line in request.Lines)
        {
            result[i++] = new RequestLineDto
            {
                Id = line.Id.Value,
                LineNo = line.LineNo,
                ItemId = line.ItemId,
                ExternalItemCode = line.ExternalItemCode,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitOfMeasureId = line.UnitOfMeasureId,
                NeedByDate = line.NeedByDate,
                SupplierName = line.SupplierName,
                SupplierContact = line.SupplierContact,
                ExternalRowReferenceId = line.ExternalRowReferenceId
            };
        }

        Array.Sort(result, (a, b) => a.LineNo.CompareTo(b.LineNo));
        return result;
    }
}
