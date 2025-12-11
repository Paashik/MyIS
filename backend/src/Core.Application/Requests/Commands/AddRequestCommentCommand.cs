using System;

namespace MyIS.Core.Application.Requests.Commands;

public class AddRequestCommentCommand
{
    /// <summary>
    /// Id заявки, к которой добавляется комментарий.
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Текущий пользователь (автор комментария). В WebApi берётся из контекста.
    /// </summary>
    public Guid AuthorId { get; init; }

    public string Text { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }
}