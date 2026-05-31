using System.Text;

namespace TheLsmArchive.Patreon.Ingestion.Helpers;

/// <summary>
/// Produces canonical lookup keys by stripping accents, punctuation, and whitespace.
/// </summary>
internal static class LookupKeyNormalizer
{
    /// <summary>
    /// Normalizes a value into a canonical, accent-free, lowercase, alphanumeric key
    /// suitable for deduplication lookups.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>A lowercase alphanumeric string with accents and non-letter/digit characters removed.</returns>
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();

        // 1. Decompose characters with accents into base + mark (e.g. 'é' -> 'e' + '´')
        string normalizedString = trimmed.Normalize(NormalizationForm.FormD);

        // 2. Filter out the non-spacing marks (accents) and keep only alphanumeric chars
        char[] alphanumericLowered =
        [
            .. normalizedString
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
        ];

        return new string(alphanumericLowered);
    }
}
