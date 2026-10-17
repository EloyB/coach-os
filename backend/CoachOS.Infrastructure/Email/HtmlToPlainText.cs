using System.Net;
using System.Text.RegularExpressions;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Leidt de text/plain-variant van een transactionele mail af uit de gerenderde
/// (MJML-)HTML. Mails met alleen een HTML-deel scoren slechter bij spamfilters
/// (MIME_HTML_ONLY), en sommige clients tonen bewust enkel platte tekst.
/// Bewust een lichte regex-aanpak: de input is altijd onze eigen template-output.
/// </summary>
internal static partial class HtmlToPlainText
{
    public static string Convert(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // 1. Onzichtbare delen weg: comments (incl. MSO-conditionals), head, style, script.
        //    "<!--[if !mso]><!-->...<!--<![endif]-->" laat zo de inhoud ertussen staan.
        string text = CommentRegex().Replace(html, string.Empty);
        text = InvisibleBlockRegex().Replace(text, string.Empty);

        // 2. Links → "label (url)", of enkel de url als label en url gelijk zijn.
        text = LinkRegex().Replace(text, FormatLink);

        // 3. Regeleindes voor <br> en block-elementen; bestaande regeleindes in de bron
        //    zijn opmaak van de HTML en tellen dus als gewone witruimte.
        text = text.Replace("\r", " ").Replace("\n", " ");
        text = LineBreakTagRegex().Replace(text, "\n");
        text = BlockTagRegex().Replace(text, "\n");

        // 4. Resterende tags weg en entities decoderen (exact één keer).
        text = TagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');

        return CollapseWhitespace(text);
    }

    private static string FormatLink(Match match)
    {
        string href = match.Groups["href"].Value.Trim();
        string label = TagRegex().Replace(match.Groups["label"].Value, string.Empty);

        string decodedHref = WebUtility.HtmlDecode(href);
        string decodedLabel = WebUtility.HtmlDecode(label).Trim();

        if (decodedLabel.Length == 0) return href;
        if (string.Equals(decodedLabel, decodedHref, StringComparison.OrdinalIgnoreCase)
            || string.Equals("mailto:" + decodedLabel, decodedHref, StringComparison.OrdinalIgnoreCase))
            return label;

        return $"{label} ({href})";
    }

    private static string CollapseWhitespace(string text)
    {
        IEnumerable<string> lines = text
            .Split('\n')
            .Select(line => HorizontalWhitespaceRegex().Replace(line, " ").Trim());

        string joined = string.Join("\n", lines);
        // Maximaal één lege regel tussen blokken.
        return ExcessNewlinesRegex().Replace(joined, "\n\n").Trim();
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"<(head|style|script|title)\b[^>]*>.*?</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex InvisibleBlockRegex();

    [GeneratedRegex(@"<a\b[^>]*?\bhref\s*=\s*(?:""(?<href>[^""]*)""|'(?<href>[^']*)')[^>]*>(?<label>.*?)</a\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTagRegex();

    [GeneratedRegex(@"</?(p|div|tr|table|h[1-6]|li|ul|ol)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"[ \t\f\v]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessNewlinesRegex();
}
