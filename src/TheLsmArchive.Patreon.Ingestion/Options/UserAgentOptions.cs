using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.Patreon.Ingestion.Options;

/// <summary>
/// The user agent options.
/// </summary>
public record UserAgentOptions
{
    /// <summary>
    /// Gets the user agent string.
    /// </summary>
    [Required]
    public required string UserAgent { get; init; }

    /// <summary>
    /// Gets the version string.
    /// </summary>
    [Required]
    public required string Version { get; init; }
}
