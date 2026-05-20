using System.Text.RegularExpressions;

namespace CoachOS.Application.Common;

public static partial class InputSanitizer
{
    [GeneratedRegex(@"[<>]|&#|javascript:|on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlOrScriptRegex();

    public static bool IsFreeOfHtml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        return !HtmlOrScriptRegex().IsMatch(value);
    }

    public static bool IsFreeOfHtmlNullable(string? value) => IsFreeOfHtml(value);
}
