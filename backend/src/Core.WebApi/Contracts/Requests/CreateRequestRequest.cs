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

    /// <summary>
    /// Внешняя ссылка (например, идентификатор объекта в Компонент‑2020).
    /// </summary>
    public string? ExternalReferenceId { get; init; }
}