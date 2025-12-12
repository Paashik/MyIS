using System;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Domain.Requests.Entities;

/// <summary>
/// Конфигурация переходов workflow для заявок.
/// Хранится в БД в таблице requests.request_transitions и используется Application-слоем.
/// </summary>
public class RequestTransition
{
    public Guid Id { get; private set; }

    public RequestTypeId RequestTypeId { get; private set; }

    public RequestStatusCode FromStatusCode { get; private set; }

    public RequestStatusCode ToStatusCode { get; private set; }

    public string ActionCode { get; private set; } = null!;

    public string? RequiredPermission { get; private set; }

    public bool IsEnabled { get; private set; }

    private RequestTransition()
    {
        // For EF Core
    }

    public RequestTransition(
        Guid id,
        RequestTypeId requestTypeId,
        RequestStatusCode fromStatusCode,
        RequestStatusCode toStatusCode,
        string actionCode,
        string? requiredPermission,
        bool isEnabled = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (requestTypeId.Value == Guid.Empty) throw new ArgumentException("RequestTypeId cannot be empty.", nameof(requestTypeId));
        if (string.IsNullOrWhiteSpace(actionCode)) throw new ArgumentException("ActionCode is required.", nameof(actionCode));

        Id = id;
        RequestTypeId = requestTypeId;
        FromStatusCode = fromStatusCode;
        ToStatusCode = toStatusCode;
        ActionCode = actionCode.Trim();
        RequiredPermission = string.IsNullOrWhiteSpace(requiredPermission) ? null : requiredPermission.Trim();
        IsEnabled = isEnabled;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void ChangeToStatus(RequestStatusCode toStatusCode)
    {
        ToStatusCode = toStatusCode;
    }

    public void ChangeRequiredPermission(string? requiredPermission)
    {
        RequiredPermission = string.IsNullOrWhiteSpace(requiredPermission) ? null : requiredPermission.Trim();
    }
}

