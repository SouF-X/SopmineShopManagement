using System.Globalization;

namespace SopmineWorkshop.Domain.Settings;

public static class DocumentReferenceFormat
{
    public static string NormalizeDateFormat(string? format) => format switch
    {
        "none" or "MM" or "yyMM" or "yyyyMM" => format,
        _ => "MM"
    };

    public static string BuildPrefix(string root, string? dateFormat, DateTime documentDate)
    {
        var datePart = NormalizeDateFormat(dateFormat) switch
        {
            "none" => string.Empty,
            "yyMM" => documentDate.ToString("yyMM", CultureInfo.InvariantCulture),
            "yyyyMM" => documentDate.ToString("yyyyMM", CultureInfo.InvariantCulture),
            _ => documentDate.ToString("MM", CultureInfo.InvariantCulture)
        };

        return string.Join('-', new[] { root.Trim(), datePart }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public static string BuildReference(string prefix, long sequence, int incrementSize) =>
        $"{prefix}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(Math.Clamp(incrementSize, 1, 8), '0')}";

    public static long? TryReadSequence(string? reference, string prefix)
    {
        var value = reference?.Trim();
        var expectedPrefix = $"{prefix}-";
        if (string.IsNullOrEmpty(value) || !value.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return null;

        var suffix = value[expectedPrefix.Length..];
        return suffix.Length > 0 && suffix.All(char.IsAsciiDigit) &&
               long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : null;
    }
}
