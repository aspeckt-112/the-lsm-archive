using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;

namespace TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

/// <summary>
/// The AI summary service interface.
/// </summary>
/// <remarks>
/// The abstraction exists to allow for different AI providers to be implemented in the future - or if there's
/// a reason to key a show summary to a specific provider.
/// </remarks>
public interface IAiSummaryService
{
    /// <summary>
    /// Generates an AI summary for the given show and Patreon post.
    /// </summary>
    /// <param name="show">The show entity.</param>
    /// <param name="patreonPost">The Patreon post entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="knownHosts">Optional list of known hosts for context.</param>
    /// <param name="knownTopics">Optional list of known topics for context.</param>
    /// <returns>The generated AI summary.</returns>
    public Task<AiSummary> GenerateAiSummaryFromPatreonPost(
        ShowEntity show,
        PatreonPostEntity patreonPost,
        CancellationToken cancellationToken,
        IEnumerable<string>? knownHosts = null,
        IEnumerable<string>? knownTopics = null);
}
