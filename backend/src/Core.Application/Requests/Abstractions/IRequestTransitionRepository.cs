using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Abstractions;

public interface IRequestTransitionRepository
{
    Task<IReadOnlyList<RequestTransition>> GetByTypeAndFromStatusAsync(
        RequestTypeId requestTypeId,
        RequestStatusCode fromStatusCode,
        CancellationToken cancellationToken);

    Task<RequestTransition?> FindByTypeFromStatusAndActionAsync(
        RequestTypeId requestTypeId,
        RequestStatusCode fromStatusCode,
        string actionCode,
        CancellationToken cancellationToken);
}

