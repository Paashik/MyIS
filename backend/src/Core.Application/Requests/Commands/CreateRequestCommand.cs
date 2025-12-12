using System;

namespace MyIS.Core.Application.Requests.Commands;

public class CreateRequestCommand
{
    /// <summary>
    /// Инициатор (текущий пользователь). В WebApi должен браться из контекста аутентификации.
    /// </summary>
    public Guid InitiatorId { get; init; }

    public Guid RequestTypeId { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public DateTimeOffset? DueDate { get; init; }

    /// <summary>
    /// Тип связанной сущности в MyIS (например, "CustomerOrder", "ProductionOrder").
    /// На Iteration 1 используется как произвольная строка без строгой типизации.
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// Идентификатор связанной сущности в MyIS.
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    /// <summary>
    /// Внешняя ссылка (например, идентификатор объекта в Компонент‑2020).
    /// Полноценная интеграция будет реализована на следующих итерациях.
    /// </summary>
    public string? ExternalReferenceId { get; init; }

    /// <summary>
    /// Позиционное тело заявки (replace-all стратегия v0.1).
    /// </summary>
    public Dto.RequestLineInputDto[]? Lines { get; init; }
}
