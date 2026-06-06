using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.Web.Api.Options;

/// <summary>
/// Configuration options for the API CORS policy.
/// </summary>
public record CorsSettingsOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Gets the allowed origins for non-development environments.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string[] AllowedOrigins { get; init; }
}
