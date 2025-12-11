using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestComment
{
    public Guid Id { get; private set; }

    public RequestId RequestId { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Text { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public Request? Request { get; private set; }

    private RequestComment()
    {
        // For EF Core
    }

    private RequestComment(
        Guid id,
        RequestId requestId,
        Guid authorId,
        string text,
        DateTimeOffset createdAt)
    {
        if (requestId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestId cannot be empty.", nameof(requestId));
        }

        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", nameof(text));
        }

        Id = id;
        RequestId = requestId;
        AuthorId = authorId;
        Text = text.Trim();
        CreatedAt = createdAt;
    }

    public static RequestComment Create(
        RequestId requestId,
        Guid authorId,
        string text,
        DateTimeOffset createdAt)
    {
        return new RequestComment(
            Guid.NewGuid(),
            requestId,
            authorId,
            text,
            createdAt);
    }
}