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

    Task<IReadOnlyList<RequestTransition>> GetAllByTypeAsync(
        RequestTypeId requestTypeId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestTransition>> GetAllAsync(
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task ReplaceForTypeAsync(
        RequestTypeId requestTypeId,
        IReadOnlyList<RequestTransition> newTransitions,
        CancellationToken cancellationToken);

    Task<bool> AnyUsesStatusCodeAsync(RequestStatusCode statusCode, CancellationToken cancellationToken);
}

