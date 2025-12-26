using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Abstractions;

public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);

    Task AddAsync(Request request, CancellationToken cancellationToken);

    Task UpdateAsync(Request request, CancellationToken cancellationToken);

    Task DeleteAsync(RequestId id, CancellationToken cancellationToken);

    /// <summary>
    /// Поиск заявок с базовой пагинацией и простыми фильтрами.
    /// Возвращает кортеж: список сущностей и общее количество строк под фильтром.
    /// </summary>
    Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(
        Guid? requestTypeId,
        Guid? requestStatusId,
        RequestDirection? direction,
        Guid? initiatorId,
        bool onlyMine,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> AnyWithTypeIdAsync(RequestTypeId requestTypeId, CancellationToken cancellationToken);

    Task<bool> AnyWithStatusIdAsync(RequestStatusId requestStatusId, CancellationToken cancellationToken);

    Task<long> GetNextRequestNumberAsync(CancellationToken cancellationToken);
}
