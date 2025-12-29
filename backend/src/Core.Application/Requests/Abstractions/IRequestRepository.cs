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
    /// РџРѕРёСЃРє Р·Р°СЏРІРѕРє СЃ Р±Р°Р·РѕРІРѕР№ РїР°РіРёРЅР°С†РёРµР№ Рё РїСЂРѕСЃС‚С‹РјРё С„РёР»СЊС‚СЂР°РјРё.
    /// Р’РѕР·РІСЂР°С‰Р°РµС‚ РєРѕСЂС‚РµР¶: СЃРїРёСЃРѕРє СЃСѓС‰РЅРѕСЃС‚РµР№ Рё РѕР±С‰РµРµ РєРѕР»РёС‡РµСЃС‚РІРѕ СЃС‚СЂРѕРє РїРѕРґ С„РёР»СЊС‚СЂРѕРј.
    /// </summary>
    Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(
        Guid? requestTypeId,
        Guid? requestStatusId,
        RequestDirection? direction,
        Guid? managerId,
        bool onlyMine,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> AnyWithTypeIdAsync(RequestTypeId requestTypeId, CancellationToken cancellationToken);

    Task<bool> AnyWithStatusIdAsync(RequestStatusId requestStatusId, CancellationToken cancellationToken);

    Task<long> GetNextRequestNumberAsync(
        RequestDirection direction,
        int year,
        CancellationToken cancellationToken);
}



