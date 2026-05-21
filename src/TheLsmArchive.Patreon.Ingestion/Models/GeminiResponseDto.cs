namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// Data transfer object for Gemini AI response containing hosts and topics.
/// </summary>
internal class GeminiResponseDto
{
    /// <summary>
    /// The hosts mentioned in the podcast episode.
    /// </summary>
    public string[] Hosts { get; init; } = [];

    /// <summary>
    /// The guests mentioned in the podcast episode.
    /// </summary>
    public string[] Guests { get; init; } = [];

    /// <summary>
    /// The topics discussed in the podcast episode.
    /// </summary>
    public string[] Topics { get; init; } = [];
}
