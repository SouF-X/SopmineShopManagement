namespace SopmineWorkshop.Application.Features.DocumentNominations;

internal static class DocumentNominationRules
{
    private static readonly HashSet<string> SupportedDateFormats =
        ["none", "MM", "yyMM", "yyyyMM"];

    public static string NormalizeRoot(string? root)
        => root?.Trim() ?? string.Empty;

    public static string NormalizeDateFormat(string? dateFormat)
        => string.IsNullOrWhiteSpace(dateFormat) ? "MM" : dateFormat.Trim();

    public static bool IsSupportedDateFormat(string dateFormat)
        => SupportedDateFormats.Contains(dateFormat);
}
