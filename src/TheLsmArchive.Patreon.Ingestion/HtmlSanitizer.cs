using System.Net;
using System.Text.RegularExpressions;

namespace TheLsmArchive.Patreon.Ingestion;

/// <summary>
/// Utility for stripping HTML tags and entities from text.
/// </summary>
public static partial class HtmlSanitizer
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

        // Remove HTML tags using regex
        string stripped = HtmlTagRegex().Replace(input, string.Empty);

        // Decode HTML entities
        stripped = WebUtility.HtmlDecode(stripped);

        // Normalize whitespace
        stripped = WhitespaceRegex().Replace(stripped, " ").Trim();

        return stripped;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}
