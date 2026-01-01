using System;

namespace MyIS.Core.Application.Requests.Commands;

public class CreateRequestCommand
{
    /// <summary>
    /// Менеджер (текущий пользователь). В WebApi должен браться из контекста аутентификации.
    /// </summary>
    public Guid ManagerId { get; init; }

    /// <summary>
    /// Инициатор заявки (алиас для совместимости с тестами/контрактами).
    /// На текущей итерации = текущий пользователь (обычно совпадает с ManagerId).
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

    public string? RelatedEntityName { get; init; }

    /// <summary>
    /// Ссылка на внешний объект (например, Компонент-2020).
    /// </summary>
    public string? ExternalReferenceId { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    public string? TargetEntityName { get; init; }

    public string? BasisType { get; init; }

    public Guid? BasisRequestId { get; init; }

    public Guid? BasisCustomerOrderId { get; init; }

    public string? BasisDescription { get; init; }

    /// <summary>
    /// Позиционное тело заявки (replace-all стратегия v0.1).
    /// </summary>
    public Dto.RequestLineInputDto[]? Lines { get; init; }
}




