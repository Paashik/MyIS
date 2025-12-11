using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

public class RequestAttachment
{
    public Guid Id { get; private set; }

    public RequestId RequestId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string FilePath { get; private set; } = null!;

    public Guid UploadedBy { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public Request? Request { get; private set; }

    private RequestAttachment()
    {
        // For EF Core
    }

    private RequestAttachment(
        Guid id,
        RequestId requestId,
        string fileName,
        string filePath,
        Guid uploadedBy,
        DateTimeOffset uploadedAt)
    {
        if (requestId.Value == Guid.Empty)
        {
            throw new ArgumentException("RequestId cannot be empty.", nameof(requestId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("FilePath is required.", nameof(filePath));
        }

        if (uploadedBy == Guid.Empty)
        {
            throw new ArgumentException("UploadedBy cannot be empty.", nameof(uploadedBy));
        }

        Id = id;
        RequestId = requestId;
        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
    }

    public static RequestAttachment Create(
        RequestId requestId,
        string fileName,
        string filePath,
        Guid uploadedBy,
        DateTimeOffset uploadedAt)
    {
        return new RequestAttachment(
            Guid.NewGuid(),
            requestId,
            fileName,
            filePath,
            uploadedBy,
            uploadedAt);
    }
}