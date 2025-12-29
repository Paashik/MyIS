using System;
using System.ComponentModel.DataAnnotations;

namespace MyIS.Core.WebApi.Contracts.Requests;

public sealed class CreateRequestRequest
{
    /// <summary>
    /// Идентификатор типа заявки.
    /// </summary>
    [Required]
    public Guid RequestTypeId { get; init; }

    /// <summary>
    /// Заголовок заявки.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; init; } = null!;

    /// <summary>
    /// Описание заявки.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Позиционное тело заявки (v0.1: replace-all стратегия).
    /// </summary>
    public RequestLineRequest[]? Lines { get; init; }

    /// <summary>
    /// Плановая дата исполнения.
    /// </summary>
    public DateTimeOffset? DueDate { get; init; }

    /// <summary>
    /// Тип связанной сущности в MyIS.
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// Идентификатор связанной сущности в MyIS.
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    public string? RelatedEntityName { get; init; }

    public string? TargetEntityType { get; init; }

    public Guid? TargetEntityId { get; init; }

    public string? TargetEntityName { get; init; }

    /// <summary>
    /// Тип основания заявки.
    /// </summary>
    public string? BasisType { get; init; }

    /// <summary>
    /// Основание: ссылка на входящую заявку.
    /// </summary>
    public Guid? BasisRequestId { get; init; }

    /// <summary>
    /// Основание: ссылка на заказ клиента.
    /// </summary>
    public Guid? BasisCustomerOrderId { get; init; }

    /// <summary>
    /// Основание: описание (включая произвольный ввод).
    /// </summary>
    public string? BasisDescription { get; init; }
}
