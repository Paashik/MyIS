using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries.Admin;
using MyIS.Core.Domain.Requests.Entities;

namespace MyIS.Core.Application.Requests.Handlers.Admin;

public sealed class GetAdminRequestTypesHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;

    public GetAdminRequestTypesHandler(IRequestTypeRepository requestTypeRepository)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
    }

    public async Task<IReadOnlyList<RequestTypeDto>> Handle(GetAdminRequestTypesQuery query, CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (query.CurrentUserId == Guid.Empty) throw new ArgumentException("CurrentUserId is required.", nameof(query));

        var types = await _requestTypeRepository.GetAllAsync(includeInactive: true, cancellationToken);

        var dtos = new List<RequestTypeDto>(types.Count);
        foreach (var t in types)
        {
            dtos.Add(MapToDto(t));
        }

        return dtos;
    }

    private static RequestTypeDto MapToDto(RequestType type)
    {
        return new RequestTypeDto
        {
            Id = type.Id.Value,
            Code = type.Code,
            Name = type.Name,
            Direction = type.Direction.ToString(),
            Description = type.Description,
            IsActive = type.IsActive
        };
    }
}

