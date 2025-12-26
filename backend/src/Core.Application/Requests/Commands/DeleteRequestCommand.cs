using System;

namespace MyIS.Core.Application.Requests.Commands;

public class DeleteRequestCommand
{
    /// <summary>
    /// Id заявки, которую нужно удалить.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Текущий пользователь (инициатор удаления). В WebApi берётся из контекста.
    /// </summary>
    public Guid CurrentUserId { get; init; }
}