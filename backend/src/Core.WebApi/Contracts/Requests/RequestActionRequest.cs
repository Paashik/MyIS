using System.ComponentModel.DataAnnotations;

namespace MyIS.Core.WebApi.Contracts.Requests;

public sealed class RequestActionRequest
{
    [MaxLength(4000)]
    public string? Comment { get; init; }
}

