using System;
using MyIS.Core.Application.Requests.Dto;

namespace MyIS.Core.Application.Requests.Queries;

public class GetRequestByIdQuery
{
    /// <summary>
    /// Идентификатор заявки.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Текущий пользователь, от имени которого выполняется запрос.
    /// Используется AccessChecker-ом.
    /// </summary>
    public Guid CurrentUserId { get; init; }
}

public sealed class GetRequestByIdResult
{
    public RequestDto Request { get; }

    public GetRequestByIdResult(RequestDto request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }
}