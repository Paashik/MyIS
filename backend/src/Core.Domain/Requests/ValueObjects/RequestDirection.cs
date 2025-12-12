namespace MyIS.Core.Domain.Requests.ValueObjects;

/// <summary>
/// Направление заявки/типа заявки.
/// Нужен в UI для верхнеуровневых вкладок Incoming/Outgoing.
/// </summary>
public enum RequestDirection
{
    Incoming = 0,
    Outgoing = 1
}

