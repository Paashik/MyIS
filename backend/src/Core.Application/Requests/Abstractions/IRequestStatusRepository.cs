using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Abstractions;

public interface IRequestStatusRepository
{
    Task<RequestStatus?> GetByIdAsync(RequestStatusId id, CancellationToken cancellationToken);

    Task<RequestStatus?> GetByCodeAsync(RequestStatusCode code, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatus>> GetAllAsync(CancellationToken cancellationToken);
}