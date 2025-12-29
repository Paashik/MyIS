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
        if (command.ManagerId == Guid.Empty)
        {
            throw new ArgumentException("ManagerId is required.", nameof(command));
        }

        if (command.RequestTypeId == Guid.Empty)
        {
            throw new ArgumentException("RequestTypeId is required.", nameof(command));
        }

        // 1. Р—Р°РіСЂСѓР·РєР° С‚РёРїР° Р·Р°СЏРІРєРё
        var typeId = new RequestTypeId(command.RequestTypeId);
        var requestType = await _requestTypeRepository.GetByIdAsync(typeId, cancellationToken);
        if (requestType is null)
        {
            throw new InvalidOperationException($"RequestType with id '{command.RequestTypeId}' was not found.");
        }

        // 2. Р—Р°РіСЂСѓР·РєР° СЃС‚Р°СЂС‚РѕРІРѕРіРѕ СЃС‚Р°С‚СѓСЃР° (Draft)
        var draftStatus = await _requestStatusRepository.GetByCodeAsync(RequestStatusCode.Draft, cancellationToken);
        if (draftStatus is null)
        {
            throw new InvalidOperationException("Initial RequestStatus 'Draft' is not configured.");
        }

        // 3. РџСЂРѕРІРµСЂРєР° РїСЂР°РІ
        await _accessChecker.EnsureCanCreateAsync(command.ManagerId, requestType, cancellationToken);

        // 4. РЎРѕР·РґР°РЅРёРµ РґРѕРјРµРЅРЅРѕР№ СЃСѓС‰РЅРѕСЃС‚Рё
        var now = DateTimeOffset.UtcNow;

        var number = await _requestRepository.GetNextRequestNumberAsync(
            requestType.Direction,
            now.Year,
            cancellationToken);
        var yearSuffix = (now.Year % 100).ToString("00", CultureInfo.InvariantCulture);
        var prefix = requestType.Direction == RequestDirection.Incoming ? "ВХ-" : "ИСХ-";
        var requestNumber = string.Concat(prefix, number.ToString("0000", CultureInfo.InvariantCulture), "-", yearSuffix);

        var request = Request.Create(
            requestType,
            draftStatus,
            command.ManagerId,
            requestNumber,
            command.Description,
            now,
            command.DueDate,
            command.RelatedEntityType,
            command.RelatedEntityId,
            command.RelatedEntityName,
            command.TargetEntityType,
            command.TargetEntityId,
            command.TargetEntityName,
            command.BasisType,
            command.BasisRequestId,
            command.BasisCustomerOrderId,
            command.BasisDescription);

        // 4.1. РџРѕР·РёС†РёРѕРЅРЅРѕРµ С‚РµР»Рѕ (v0.1: replace-all)
        if (command.Lines is not null)
        {
            var lines = MapToDomainLines(request.Id, command.Lines);
            request.ReplaceLines(lines, now, isCurrentStatusFinal: false);
        }

        // 5. РЎРѕС…СЂР°РЅРµРЅРёРµ РІ СЂРµРїРѕР·РёС‚РѕСЂРёР№
        await _requestRepository.AddAsync(request, cancellationToken);

        // 6. РњР°РїРїРёРЅРі РІ DTO
        var manager = await _userRepository.GetByIdAsync(request.ManagerId, cancellationToken);
        var managerBaseName = manager?.Employee?.ShortName ?? manager?.Employee?.FullName ?? manager?.FullName ?? manager?.Login;
        var managerFullName = PersonNameFormatter.ToShortName(managerBaseName) ?? managerBaseName;

        var dto = MapToDto(request, requestType, draftStatus, managerFullName);

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





