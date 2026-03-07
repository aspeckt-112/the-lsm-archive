using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.Patreon.Ingestion.Options;

/// <summary>
/// The RSS feed options, a collection of RSS feed sources.
/// </summary>
public sealed class RssFeedSources : List<RssFeedSource>;

/// <summary>
/// The RSS feed source.
/// </summary>
public record RssFeedSource
{
    /// <summary>
    /// The name of the RSS feed.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Name { get; init; }

    /// <summary>
    /// The URL of the RSS feed.
    /// </summary>
    /// <remarks>
    /// Note, this is set via user secrets in development and environment variables in production.
    /// </remarks>
    [Required]
    [Url]
    public required string Url { get; init; }
}

