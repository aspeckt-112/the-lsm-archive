using System.Xml.Linq;

namespace TheLsmArchive.Patreon.Ingestion;

/// <summary>
/// Extension methods for the Patreon Ingestion service.
/// </summary>
internal static class Extensions
{
    extension(XElement? element)
    {
        /// <summary>
        /// Gets the trimmed value of the specified child element.
        /// </summary>
        /// <param name="name">The name of the child element.</param>
        /// <returns>The trimmed value of the child element.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the element is missing or empty.</exception>
        internal string Get(XName name)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(name);

            string? value = element.Element(name)?.Value.Trim();

            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException(
                    $"Missing or empty value for element '{name}' under '{element.Name}'.")
                : value;
        }

        /// <summary>
        /// Gets the <see cref="DateTimeOffset"/> value of the current element.
        /// </summary>
        /// <returns>The <see cref="DateTimeOffset"/> value, or <c>null</c> if the element is missing or empty.</returns>
        internal DateTimeOffset? GetDateTimeOffset()
        {
            if (element == null || string.IsNullOrWhiteSpace(element.Value))
            {
                return null;
            }

            string dateString = element.Value.Trim();

            string[] dateFormats =
            [
                "ddd, dd MMM yyyy HH':'mm':'ss zzz",
                "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'",
                "dd MMM yyyy HH':'mm':'ss zzz"
            ];

            foreach (string dateFormat in dateFormats)
            {
                if (DateTimeOffset.TryParseExact(
                        dateString,
                        dateFormat,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                        out DateTimeOffset result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}
