using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Abstractions;

public interface IRequestCommentRepository
{
    Task<IReadOnlyList<RequestComment>> GetByRequestIdAsync(
        RequestId requestId,
        CancellationToken cancellationToken);
}
