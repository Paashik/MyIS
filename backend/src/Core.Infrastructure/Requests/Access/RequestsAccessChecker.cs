using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Infrastructure.Requests.Access;

/// <summary>
/// РЈРїСЂРѕС‰С‘РЅРЅС‹Р№ AccessChecker РґР»СЏ РјРѕРґСѓР»СЏ Requests РЅР° Iteration 1.
///
/// РџСЂР°РІРёР»Рѕ: Р»СЋР±РѕРјСѓ Р°СѓС‚РµРЅС‚РёС„РёС†РёСЂРѕРІР°РЅРЅРѕРјСѓ РїРѕР»СЊР·РѕРІР°С‚РµР»СЋ (Guid != Empty)
/// СЂР°Р·СЂРµС€РµРЅС‹ РІСЃРµ Р±Р°Р·РѕРІС‹Рµ РѕРїРµСЂР°С†РёРё. РџРѕР»РЅРѕС†РµРЅРЅР°СЏ СЂРѕР»РµРІР°СЏ РјРѕРґРµР»СЊ Рё
/// РјР°С‚СЂРёС†Р° РїСЂР°РІ Р±СѓРґСѓС‚ РґРѕР±Р°РІР»РµРЅС‹ РЅР° СЃР»РµРґСѓСЋС‰РёС… РёС‚РµСЂР°С†РёСЏС….
/// </summary>
public sealed class RequestsAccessChecker : IRequestsAccessChecker
{
    private static void EnsureAuthenticated(Guid currentUserId)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User must be authenticated to perform this operation.");
        }
    }

    public Task EnsureCanCreateAsync(
        Guid currentUserId,
        RequestType requestType,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (requestType is null) throw new ArgumentNullException(nameof(requestType));

        // TODO: РґРѕР±Р°РІРёС‚СЊ РїСЂРѕРІРµСЂРєСѓ СЂРѕР»РµР№/РїСЂР°РІ (manager, Admin Рё С‚.Рї.) РЅР° СЃР»РµРґСѓСЋС‰РёС… РёС‚РµСЂР°С†РёСЏС….
        return Task.CompletedTask;
    }

    public Task EnsureCanViewAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: РІ Р±СѓРґСѓС‰РµРј РјРѕР¶РЅРѕ РѕРіСЂР°РЅРёС‡РёРІР°С‚СЊ РїСЂРѕСЃРјРѕС‚СЂ (РЅР°РїСЂРёРјРµСЂ, С‚РѕР»СЊРєРѕ СЃРІРѕРё Р·Р°СЏРІРєРё
        // РёР»Рё РїРѕ СЂРѕР»СЏРј Approver/Executor). РќР° Iteration 1 СЂР°Р·СЂРµС€Р°РµРј РІСЃРµРј.
        return Task.CompletedTask;
    }

    public Task EnsureCanUpdateAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: РІ Р±СѓРґСѓС‰РµРј РјРѕР¶РЅРѕ РѕРіСЂР°РЅРёС‡РёРІР°С‚СЊ РёР·РјРµРЅРµРЅРёРµ С‚РѕР»СЊРєРѕ РёРЅРёС†РёР°С‚РѕСЂРѕРј РёР»Рё Р°РґРјРёРЅРёСЃС‚СЂР°С‚РѕСЂРѕРј.
        return Task.CompletedTask;
    }

    public Task EnsureCanEditBodyAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: РґРѕР±Р°РІРёС‚СЊ RBAC/permissions (Requests.EditBody) РЅР° СЃР»РµРґСѓСЋС‰РёС… РёС‚РµСЂР°С†РёСЏС….
        return Task.CompletedTask;
    }

    public Task EnsureCanPerformActionAsync(
        Guid currentUserId,
        Request request,
        string actionCode,
        string? requiredPermission,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(actionCode)) throw new ArgumentException("ActionCode is required.", nameof(actionCode));

        // TODO: requiredPermission РґРѕР»Р¶РµРЅ РїСЂРѕРІРµСЂСЏС‚СЊСЃСЏ С‡РµСЂРµР· СЂР°СЃС€РёСЂСЏРµРјСѓСЋ permission-РјРѕРґРµР»СЊ.
        // РќР° С‚РµРєСѓС‰РµР№ РёС‚РµСЂР°С†РёРё СЂР°Р·СЂРµС€Р°РµРј РІСЃРµ РґРµР№СЃС‚РІРёСЏ Р°СѓС‚РµРЅС‚РёС„РёС†РёСЂРѕРІР°РЅРЅРѕРјСѓ РїРѕР»СЊР·РѕРІР°С‚РµР»СЋ.
        _ = requiredPermission;
        return Task.CompletedTask;
    }

    public Task EnsureCanAddCommentAsync(
        Guid currentUserId,
        RequestId requestId,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (requestId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestId cannot be empty.", nameof(requestId));
        }

        // TODO: РІ Р±СѓРґСѓС‰РµРј РјРѕР¶РЅРѕ Р·Р°РїСЂРµС‚РёС‚СЊ РєРѕРјРјРµРЅС‚Р°СЂРёРё РґР»СЏ РЅРµРєРѕС‚РѕСЂС‹С… СЃС‚Р°С‚СѓСЃРѕРІ/СЂРѕР»РµР№.
        return Task.CompletedTask;
    }

    public Task EnsureCanReadReferenceDataAsync(
        Guid currentUserId,
        string referenceDataScope,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (string.IsNullOrWhiteSpace(referenceDataScope))
        {
            throw new ArgumentException("Reference data scope must be provided.", nameof(referenceDataScope));
        }

        // TODO: РїСЂРёРІСЏР·Р°С‚СЊ Рє permissions (РЅР°РїСЂРёРјРµСЂ, Requests.ViewReferenceData) РЅР° СЃР»РµРґСѓСЋС‰РёС… РёС‚РµСЂР°С†РёСЏС….
        return Task.CompletedTask;
    }

    public Task EnsureCanDeleteAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // РќР° С‚РµРєСѓС‰РµР№ РёС‚РµСЂР°С†РёРё СЂР°Р·СЂРµС€Р°РµРј СѓРґР°Р»РµРЅРёРµ С‚РѕР»СЊРєРѕ РёРЅРёС†РёР°С‚РѕСЂСѓ Р·Р°СЏРІРєРё.
        if (request.ManagerId != currentUserId)
        {
            throw new UnauthorizedAccessException("Only the manager can delete the request.");
        }

        // TODO: РІ Р±СѓРґСѓС‰РµРј РјРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ РїСЂРѕРІРµСЂРєРё (РЅР°РїСЂРёРјРµСЂ, СЃС‚Р°С‚СѓСЃ РЅРµ С„РёРЅР°Р»СЊРЅС‹Р№).
        return Task.CompletedTask;
    }
}




