using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestTypesHandler
{
    private readonly IRequestTypeRepository _requestTypeRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestTypesHandler(
        IRequestTypeRepository requestTypeRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestTypeRepository = requestTypeRepository ?? throw new ArgumentNullException(nameof(requestTypeRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<GetRequestTypesResult> Handle(
        GetRequestTypesQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        await _accessChecker.EnsureCanReadReferenceDataAsync(
            query.CurrentUserId,
            nameof(RequestType),
            cancellationToken);

        var types = await _requestTypeRepository.GetAllAsync(includeInactive: false, cancellationToken);

        var dtos = new List<RequestTypeDto>(types.Count);
        foreach (var t in types)
        {
            dtos.Add(MapToDto(t));
        }

        return new GetRequestTypesResult(dtos);
    }

    private static RequestTypeDto MapToDto(RequestType type)
    {
        return new RequestTypeDto
        {
            Id = type.Id.Value,
            Name = type.Name,
            Direction = type.Direction.ToString(),
            Description = type.Description,
            IsActive = type.IsActive
        };
    }
}
