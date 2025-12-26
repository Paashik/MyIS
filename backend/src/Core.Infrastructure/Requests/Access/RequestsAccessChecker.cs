using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Infrastructure.Requests.Access;

/// <summary>
/// Упрощённый AccessChecker для модуля Requests на Iteration 1.
///
/// Правило: любому аутентифицированному пользователю (Guid != Empty)
/// разрешены все базовые операции. Полноценная ролевая модель и
/// матрица прав будут добавлены на следующих итерациях.
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

        // TODO: добавить проверку ролей/прав (Initiator, Admin и т.п.) на следующих итерациях.
        return Task.CompletedTask;
    }

    public Task EnsureCanViewAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: в будущем можно ограничивать просмотр (например, только свои заявки
        // или по ролям Approver/Executor). На Iteration 1 разрешаем всем.
        return Task.CompletedTask;
    }

    public Task EnsureCanUpdateAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: в будущем можно ограничивать изменение только инициатором или администратором.
        return Task.CompletedTask;
    }

    public Task EnsureCanEditBodyAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // TODO: добавить RBAC/permissions (Requests.EditBody) на следующих итерациях.
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

        // TODO: requiredPermission должен проверяться через расширяемую permission-модель.
        // На текущей итерации разрешаем все действия аутентифицированному пользователю.
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

        // TODO: в будущем можно запретить комментарии для некоторых статусов/ролей.
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

        // TODO: привязать к permissions (например, Requests.ViewReferenceData) на следующих итерациях.
        return Task.CompletedTask;
    }

    public Task EnsureCanDeleteAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUserId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        // На текущей итерации разрешаем удаление только инициатору заявки.
        if (request.InitiatorId != currentUserId)
        {
            throw new UnauthorizedAccessException("Only the initiator can delete the request.");
        }

        // TODO: в будущем можно добавить дополнительные проверки (например, статус не финальный).
        return Task.CompletedTask;
    }
}
