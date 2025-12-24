using System;
using System.Collections.Generic;
using System.Globalization;
using MyIS.Core.Domain.Mdm.Entities;

namespace MyIS.Core.Domain.Mdm.Services;

public static class ItemNomenclature
{
    private static readonly IReadOnlyDictionary<ItemKind, string> DefaultPrefixByKind =
        new Dictionary<ItemKind, string>
        {
            [ItemKind.Component] = "CMP",
            [ItemKind.Material] = "MAT",
            [ItemKind.Assembly] = "ASM",
            [ItemKind.Product] = "PRD",
            [ItemKind.Service] = "SRV",
            [ItemKind.Tool] = "TOL",
            [ItemKind.Equipment] = "EQP"
        };

    public static string GetDefaultPrefix(ItemKind itemKind) =>
        DefaultPrefixByKind.TryGetValue(itemKind, out var prefix) ? prefix : "UNK";

    public static string FormatNomenclatureNo(string prefix, int number) =>
        $"{prefix}-{number.ToString("D6", CultureInfo.InvariantCulture)}";

    public static bool TryExtractNumericSuffix(string nomenclatureNo, string prefix, out int number)
    {
        number = 0;
        var prefixWithDash = $"{prefix}-";
        if (!nomenclatureNo.StartsWith(prefixWithDash, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = nomenclatureNo.Substring(prefixWithDash.Length);
        if (suffix.Length != 6)
        {
            return false;
        }

        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    public static bool TryParseComponentCode(string? raw, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return int.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }
}

