using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

/// <summary>
/// The metadata extraction service interface.
/// </summary>
/// <remarks>
/// The abstraction exists to allow for different AI providers to be implemented in the future - or if there is
/// a reason to key show metadata extraction to a specific provider.
/// </remarks>
public interface IMetadataExtractionService
{
    /// <summary>
    /// Extracts structured metadata for the given show and Patreon post.
    /// </summary>
    /// <param name="show">The show entity.</param>
    /// <param name="patreonPost">The Patreon post entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="knownPersons">Optional list of known persons (hosts and frequent guests) for context.</param>
    /// <param name="knownTopics">Optional list of known topics for context.</param>
    /// <returns>The extracted metadata.</returns>
    public Task<AiSummary> ExtractMetadataAsync(
        ShowEntity show,
        PatreonPostEntity patreonPost,
        CancellationToken cancellationToken,
        IList<string>? knownPersons = null,
        IList<string>? knownTopics = null);
}

