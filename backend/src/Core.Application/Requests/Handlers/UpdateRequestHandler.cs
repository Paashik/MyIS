using System;
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

public class UpdateRequestHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;
    private readonly IUserRepository _userRepository;

    public UpdateRequestHandler(
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

        // 1. Р—Р°РіСЂСѓР·РєР° Р·Р°СЏРІРєРё
        var requestId = new RequestId(command.Id);
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{command.Id}' was not found.");
        }

        // 2. Р—Р°РіСЂСѓР·РєР° С‚РµРєСѓС‰РµРіРѕ С‚РёРїР° Рё СЃС‚Р°С‚СѓСЃР°
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

        // 3. РџСЂРѕРІРµСЂРєР° РїСЂР°РІ РЅР° СЂРµРґР°РєС‚РёСЂРѕРІР°РЅРёРµ
        await _accessChecker.EnsureCanUpdateAsync(command.CurrentUserId, request, cancellationToken);

        // 4. Смена типа заявки (разрешена только для Draft, чтобы не ломать workflow).
        if (command.RequestTypeId.HasValue && command.RequestTypeId.Value != request.RequestTypeId.Value)
        {
            if (!string.Equals(status.Code.Value, RequestStatusCode.Draft.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot change request type unless status is Draft.");
            }

            var newTypeId = RequestTypeId.From(command.RequestTypeId.Value);
            var newType = await _requestTypeRepository.GetByIdAsync(newTypeId, cancellationToken);
            if (newType is null)
            {
                throw new InvalidOperationException($"RequestType with id '{command.RequestTypeId.Value}' was not found.");
            }

            request.ChangeType(newType);
            requestType = newType;
        }

        // 4.1. РћР±РЅРѕРІР»РµРЅРёРµ РґРµС‚Р°Р»РµР№ (РЅРµР»СЊР·СЏ, РµСЃР»Рё СЃС‚Р°С‚СѓСЃ С„РёРЅР°Р»СЊРЅС‹Р№)
        var now = DateTimeOffset.UtcNow;
        var basisType = command.BasisType ?? request.BasisType;
        var basisRequestId = command.BasisRequestId ?? request.BasisRequestId;
        var basisCustomerOrderId = command.BasisCustomerOrderId ?? request.BasisCustomerOrderId;
        var basisDescription = command.BasisDescription ?? request.BasisDescription;

        request.UpdateDetails(
            request.Title,
            command.Description,
            command.DueDate,
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.RelatedEntityName,
            command.TargetEntityType,
            command.TargetEntityId,
            command.TargetEntityName,
            basisType,
            basisRequestId,
            basisCustomerOrderId,
            basisDescription,
            now,
            isCurrentStatusFinal: status.IsFinal);

        // 4.1. РџРѕР·РёС†РёРѕРЅРЅРѕРµ С‚РµР»Рѕ (v0.1: replace-all)
        if (command.Lines is not null)
        {
            await _accessChecker.EnsureCanEditBodyAsync(command.CurrentUserId, request, cancellationToken);
            var lines = MapToDomainLines(request.Id, command.Lines);
            request.ReplaceLines(lines, now, isCurrentStatusFinal: status.IsFinal);
        }

        // 5. РЎРѕС…СЂР°РЅРµРЅРёРµ
        await _requestRepository.UpdateAsync(request, cancellationToken);

        // 6. РњР°РїРїРёРЅРі РІ DTO
        var manager = await _userRepository.GetByIdAsync(request.ManagerId, cancellationToken);
        var managerBaseName = manager?.Employee?.ShortName ?? manager?.Employee?.FullName ?? manager?.FullName ?? manager?.Login;
        var managerFullName = PersonNameFormatter.ToShortName(managerBaseName) ?? managerBaseName;

        var dto = MapToDto(request, requestType, status, managerFullName);

        return dto;
    }

    private static RequestDto MapToDto(
        Request request,
        RequestType requestType,
        RequestStatus status,
        string? managerFullName)
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
            ManagerId = request.ManagerId,
            ManagerFullName = managerFullName,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityName = request.RelatedEntityName,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            TargetEntityName = request.TargetEntityName,
            BasisType = request.BasisType,
            BasisRequestId = request.BasisRequestId,
            BasisCustomerOrderId = request.BasisCustomerOrderId,
            BasisDescription = request.BasisDescription,
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




