namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// The AI-extracted metadata describing hosts, guests, and topics.
/// </summary>
/// <param name="Hosts">The hosts of the episode.</param>
/// <param name="Guests">The guests of the episode.</param>
/// <param name="Topics">The topics discussed in the episode.</param>
public sealed record AiSummary(
    IReadOnlyList<string> Hosts,
    IReadOnlyList<string> Guests,
    IReadOnlyList<string> Topics
);
