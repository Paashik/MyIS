using System;

namespace MyIS.Core.Infrastructure.Data.Entities.Integration;

public class Component2020Connection
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string? MdbPath { get; private set; }

    public string? Login { get; private set; }

    public string? EncryptedPassword { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? LastTestedAt { get; private set; }

    public string? LastTestMessage { get; private set; }

    public Component2020Connection()
    {
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string? mdbPath, string? login, string? passwordEncrypted, bool isActive)
    {
        MdbPath = mdbPath;
        Login = login;
        EncryptedPassword = passwordEncrypted;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTestResult(DateTimeOffset testedAt, string? message)
    {
        LastTestedAt = testedAt;
        LastTestMessage = message;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}