using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Abstractions;

/// <summary>
/// AccessChecker для модуля Requests.
/// 
/// На Iteration 1 реализуется в упрощённом виде: аутентифицированному пользователю
/// разрешены все базовые операции, детальная ролевая модель и workflow будут добавлены позже.
/// Конкретная реализация размещается в Infrastructure.
/// </summary>
public interface IRequestsAccessChecker
{
    /// <summary>
    /// Проверка права на создание новой заявки указанным пользователем.
    /// Реализация должна выбросить исключение (например, UnauthorizedAccessException)
    /// при отсутствии прав.
    /// </summary>
    Task EnsureCanCreateAsync(
        Guid currentUserId,
        RequestType requestType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверка права на просмотр заявки.
    /// </summary>
    Task EnsureCanViewAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверка права на редактирование заявки (изменение деталей, но не статуса).
    /// </summary>
    Task EnsureCanUpdateAsync(
        Guid currentUserId,
        Request request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверка права на добавление комментария к заявке.
    /// </summary>
    Task EnsureCanAddCommentAsync(
        Guid currentUserId,
        RequestId requestId,
        CancellationToken cancellationToken);
}