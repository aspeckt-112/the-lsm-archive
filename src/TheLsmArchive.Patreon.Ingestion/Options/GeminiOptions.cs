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

    /// <summary>
    /// Timeout for Gemini API calls in milliseconds. Default is 1 hour (3600000 ms).
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int TimeoutInMilliseconds { get; init; }
}
