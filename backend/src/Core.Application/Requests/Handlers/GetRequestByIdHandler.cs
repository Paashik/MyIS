using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestByIdHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestByIdHandler(
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

    public async Task<GetRequestByIdResult> Handle(
        GetRequestByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.Id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(query));
        }

        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        // 1. Загрузка заявки
        var requestId = new RequestId(query.Id);
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{query.Id}' was not found.");
        }

        // 2. Проверка прав на просмотр
        await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

        // 3. Загрузка справочников (тип и статус)
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

        // 4. Маппинг в DTO
        var dto = MapToDto(request, requestType, status, initiatorFullName: null);

        return new GetRequestByIdResult(dto);
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
