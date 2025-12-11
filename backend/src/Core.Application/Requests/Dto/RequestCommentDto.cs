using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestCommentDto
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public Guid AuthorId { get; init; }

    public string? AuthorFullName { get; init; }

    public string Text { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }
}