using System;

namespace MyIS.Core.Application.Requests.Dto;

public class RequestAttachmentDto
{
    public Guid Id { get; init; }

    public Guid RequestId { get; init; }

    public string FileName { get; init; } = null!;

    public string FilePath { get; init; } = null!;

    public Guid UploadedBy { get; init; }

    public string? UploadedByFullName { get; init; }

    public DateTimeOffset UploadedAt { get; init; }
}