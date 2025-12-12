using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries.Admin;
using MyIS.Core.Domain.Requests.Entities;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class GetAdminRequestStatusesHandler
{
    private readonly IRequestStatusRepository _requestStatusRepository;

    public GetAdminRequestStatusesHandler(IRequestStatusRepository requestStatusRepository)
    {
        _requestStatusRepository = requestStatusRepository ?? throw new ArgumentNullException(nameof(requestStatusRepository));
    }

    public async Task<IReadOnlyList<RequestStatusDto>> Handle(GetAdminRequestStatusesQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var statuses = await _requestStatusRepository.GetAllAsync(includeInactive: true, cancellationToken);

        var dtos = new List<RequestStatusDto>(statuses.Count);
        foreach (var s in statuses)
        {
            dtos.Add(MapToDto(s));
        }

        return dtos;
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

