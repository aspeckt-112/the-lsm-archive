using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

/// <summary>
/// Defines the contract for processing a parsed Patreon feed.
/// </summary>
public interface IPatreonFeedProcessingService
{
    /// <summary>
    /// Processes a single Patreon feed from ingestion through post processing.
    /// </summary>
    /// <param name="feed">The Patreon feed to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ProcessFeedAsync(PatreonFeed feed, CancellationToken cancellationToken);
}


