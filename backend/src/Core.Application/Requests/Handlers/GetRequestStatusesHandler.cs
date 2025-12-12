using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestStatusesHandler
{
    private readonly IRequestStatusRepository _requestStatusRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestStatusesHandler(
        IRequestStatusRepository requestStatusRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<GetRequestStatusesResult> Handle(
        GetRequestStatusesQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        // На Iteration 1 AccessChecker может реализовывать упрощённую логику либо быть no-op,
        // но точка расширения оставлена.
        var statuses = await _requestStatusRepository.GetAllAsync(includeInactive: false, cancellationToken);

        var dtos = new List<RequestStatusDto>(statuses.Count);
        foreach (var s in statuses)
        {
            dtos.Add(MapToDto(s));
        }

        return new GetRequestStatusesResult(dtos);
    }

    private static RequestStatusDto MapToDto(RequestStatus status)
    {
        return new RequestStatusDto
        {
            Id = status.Id.Value,
            Code = status.Code.Value,
            Name = status.Name,
            IsFinal = status.IsFinal,
            Description = status.Description,
            IsActive = status.IsActive
        };
    }
}
