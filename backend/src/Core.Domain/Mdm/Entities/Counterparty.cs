using System;

namespace MyIS.Core.Domain.Mdm.Entities;

public class Counterparty
{
    public Guid Id { get; private set; }

    public string? Code { get; private set; }

    public string Name { get; private set; }

    public string? FullName { get; private set; }

    public string? Inn { get; private set; }

    public string? Kpp { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? City { get; private set; }

    public string? Address { get; private set; }

    public string? Site { get; private set; }

    public string? SiteLogin { get; private set; }

    public string? SitePassword { get; private set; }

    public string? Note { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Counterparty()
    {
        Name = null!;
    }

    public Counterparty(
        string? code,
        string name,
        string? fullName,
        string? inn,
        string? kpp,
        string? email,
        string? phone,
        string? city,
        string? address,
        string? site,
        string? siteLogin,
        string? sitePassword,
        string? note)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        Id = Guid.NewGuid();
        Code = NormalizeOptional(code);
        Name = name.Trim();
        FullName = NormalizeOptional(fullName);
        Inn = NormalizeOptional(inn);
        Kpp = NormalizeOptional(kpp);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        City = NormalizeOptional(city);
        Address = NormalizeOptional(address);
        Site = NormalizeOptional(site);
        SiteLogin = NormalizeOptional(siteLogin);
        SitePassword = NormalizeOptional(sitePassword);
        Note = NormalizeOptional(note);
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFromExternal(
        string name,
        string? fullName,
        string? inn,
        string? kpp,
        string? email,
        string? phone,
        string? city,
        string? address,
        string? site,
        string? siteLogin,
        string? sitePassword,
        string? note,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
        FullName = NormalizeOptional(fullName);
        Inn = NormalizeOptional(inn);
        Kpp = NormalizeOptional(kpp);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        City = NormalizeOptional(city);
        Address = NormalizeOptional(address);
        Site = NormalizeOptional(site);
        SiteLogin = NormalizeOptional(siteLogin);
        SitePassword = NormalizeOptional(sitePassword);
        Note = NormalizeOptional(note);
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
