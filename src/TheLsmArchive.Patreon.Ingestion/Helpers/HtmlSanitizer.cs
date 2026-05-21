using System.Net;
using System.Text.RegularExpressions;

namespace TheLsmArchive.Patreon.Ingestion.Helpers;

/// <summary>
/// Utility for stripping HTML tags and entities from text.
/// </summary>
internal static partial class HtmlSanitizer
{
    /// <summary>
    /// Strips HTML tags from the input string, decodes HTML entities, and normalizes whitespace.
    /// </summary>
    /// <param name="input">The HTML input string.</param>
    /// <returns>The plain-text content with normalized whitespace.</returns>
    public static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string stripped = RemoveHtmlTags(input);
        stripped = WebUtility.HtmlDecode(stripped);
        stripped = RemoveHtmlTags(stripped);
        stripped = WhitespaceRegex().Replace(stripped, " ").Trim();

        return stripped;
    }

    private static string RemoveHtmlTags(string input)
    {
        string normalizedTagBoundaries = HtmlSeparatorTagRegex().Replace(input, " ");
        return HtmlTagRegex().Replace(normalizedTagBoundaries, string.Empty);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"</?(?:address|article|aside|blockquote|br|dd|div|dl|dt|figcaption|figure|footer|h[1-6]|header|hr|li|main|nav|ol|p|pre|section|table|tbody|td|tfoot|th|thead|tr|ul)(?:\s+[^<>]*)?\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlSeparatorTagRegex();

    [GeneratedRegex(@"</?(?:a|abbr|address|article|aside|audio|b|blockquote|br|code|dd|del|div|dl|dt|em|figcaption|figure|footer|h[1-6]|header|hr|i|iframe|img|ins|li|main|mark|nav|ol|p|picture|pre|section|small|source|span|strong|sub|sup|table|tbody|td|tfoot|th|thead|time|tr|u|ul|video)(?:\s+[^<>]*)?\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();
}
