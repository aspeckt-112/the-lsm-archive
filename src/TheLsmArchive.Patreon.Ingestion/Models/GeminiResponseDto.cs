namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// Data transfer object for Gemini AI response containing hosts and topics.
/// </summary>
internal class GeminiResponseDto
{
    /// <summary>
    /// The hosts mentioned in the podcast episode.
    /// </summary>
    public string[] Hosts { get; set; } = [];

    /// <summary>
    /// The topics discussed in the podcast episode.
    /// </summary>
    public string[] Topics { get; set; } = [];
}
