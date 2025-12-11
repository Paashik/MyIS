using System;
using System.ComponentModel.DataAnnotations;

namespace MyIS.Core.WebApi.Contracts.Requests;

public sealed class AddRequestCommentRequest
{
    /// <summary>
    /// Текст комментария.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string Text { get; init; } = null!;

    /// <summary>
    /// Время создания комментария на клиенте.
    /// Опционально; если не передано, будет использовано серверное время.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }
}