using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;

namespace TheLsmArchive.Patreon.Ingestion.Parsers.Abstractions;

/// <summary>
/// Defines the contract for fetching and parsing Patreon RSS feed sources.
/// </summary>
public interface IPatreonRssParser
{
    /// <summary>
    /// Fetches and parses all configured RSS feed sources.
    /// </summary>
    /// <param name="sources">The RSS feed sources to parse.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of parsed feeds.</returns>
    public IAsyncEnumerable<PatreonFeed> ParseFeedsAsync(
        IEnumerable<RssFeedSource> sources,
        CancellationToken cancellationToken = default);
}


