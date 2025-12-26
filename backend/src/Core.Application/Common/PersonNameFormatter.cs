using System;

namespace MyIS.Core.Application.Common;

public static class PersonNameFormatter
{
    public static string? ToShortName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        var surname = parts[0];
        if (parts.Length == 1) return surname;

        var initials = GetInitial(parts[1]);
        if (parts.Length >= 3)
        {
            initials += GetInitial(parts[2]);
        }

        return string.IsNullOrWhiteSpace(initials) ? surname : $"{surname} {initials}";
    }

    private static string GetInitial(string part)
    {
        if (string.IsNullOrWhiteSpace(part)) return string.Empty;
        var trimmed = part.Trim();
        return trimmed.Length > 0 ? $"{trimmed.Substring(0, 1)}." : string.Empty;
    }
}
