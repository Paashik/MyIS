using System;
using System.Collections.Generic;

namespace MyIS.Core.WebApi.Contracts.Auth;

public sealed class AuthUserDto
{
    public Guid Id { get; init; }
    public string Login { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}