using System.Globalization;

namespace Aether.Core.Validation;

/// <summary>Parses optional numeric text typed in forms.</summary>
public static class NumberParser
{
    /// <summary>
    /// Empty text gives a null value (not recorded). Otherwise the text must be a plain number;
    /// both "12.5" and "12,5" are accepted. Thousands separators are rejected.
    /// Returns false when the text is not a valid finite number.
    /// </summary>
    public static bool TryParseOptional(string? text, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;

        var s = text.Trim();
        if (s.Contains(',') && !s.Contains('.')) s = s.Replace(',', '.');

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && double.IsFinite(parsed))
        {
            value = parsed;
            return true;
        }
        return false;
    }

    /// <summary>Formats a value for editing; null becomes empty text.</summary>
    public static string Format(double? value) =>
        value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
}
