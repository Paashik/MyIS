using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Mdm.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class CreateRequestHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;
    private readonly IUserRepository _userRepository;

    public CreateRequestHandler(
        IRequestRepository requestRepository,
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestsAccessChecker accessChecker,
        IUserRepository userRepository)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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

        var number = await _requestRepository.GetNextRequestNumberAsync(cancellationToken);
        var yearSuffix = (now.Year % 100).ToString("00", CultureInfo.InvariantCulture);
        var requestNumber = string.Concat(number.ToString("0000", CultureInfo.InvariantCulture), "-", yearSuffix);

        var request = Request.Create(
            requestType,
            draftStatus,
            command.InitiatorId,
            requestNumber,
            command.Description,
            now,
            command.DueDate,
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.RelatedEntityName,
            command.ExternalReferenceId,
            command.TargetEntityType,
            command.TargetEntityId,
            command.TargetEntityName);

        // 4.1. Позиционное тело (v0.1: replace-all)
        if (command.Lines is not null)
        {
            var lines = MapToDomainLines(request.Id, command.Lines);
            request.ReplaceLines(lines, now, isCurrentStatusFinal: false);
        }

        // 5. Сохранение в репозиторий
        await _requestRepository.AddAsync(request, cancellationToken);

        // 6. Маппинг в DTO
        var initiator = await _userRepository.GetByIdAsync(request.InitiatorId, cancellationToken);
        var initiatorBaseName = initiator?.Employee?.ShortName ?? initiator?.Employee?.FullName ?? initiator?.FullName ?? initiator?.Login;
        var initiatorFullName = PersonNameFormatter.ToShortName(initiatorBaseName) ?? initiatorBaseName;

        var dto = MapToDto(request, requestType, draftStatus, initiatorFullName);

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
            RequestTypeName = requestType.Name,
            RequestStatusId = status.Id.Value,
            RequestStatusCode = status.Code.Value,
            RequestStatusName = status.Name,
            InitiatorId = request.InitiatorId,
            InitiatorFullName = initiatorFullName,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityName = request.RelatedEntityName,
            ExternalReferenceId = request.ExternalReferenceId,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            TargetEntityName = request.TargetEntityName,
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
                l.ItemId.HasValue ? ItemId.From(l.ItemId.Value) : null,
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
                ItemId = line.ItemId?.Value,
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
