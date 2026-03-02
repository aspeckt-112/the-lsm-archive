using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.Patreon.Ingestion.Options;

/// <summary>
/// The Gemini options.
/// </summary>
public record GeminiOptions
{
    /// <summary>
    /// Gemini API key.
    /// </summary>
    [Required]
    public required string ApiKey { get; init; }

    /// <summary>
    /// Gemini model to use.
    /// </summary>
    [Required]
    public required string Model { get; init; }
}
