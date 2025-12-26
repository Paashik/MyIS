using System;

namespace MyIS.Core.Application.Requests.Commands;

public class UpdateRequestCommand
{
    /// <summary>
    /// Id заявки, которую нужно изменить.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Текущий пользователь (инициатор изменения). В WebApi берётся из контекста.
    /// </summary>
    public Guid CurrentUserId { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityName { get; init; }

    public string? ExternalReferenceId { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    public string? TargetEntityName { get; init; }

    /// <summary>
    /// Позиционное тело заявки (replace-all стратегия v0.1).
    /// Если null — строки не трогаем; если задано (в т.ч. пустой массив) — заменяем целиком.
    /// </summary>
    public Dto.RequestLineInputDto[]? Lines { get; init; }
}
