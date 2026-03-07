using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.ApiClient.Options;

/// <summary>
/// The API options.
/// </summary>
public record ApiOptions
{
    /// <summary>
    /// Gets the base URL
    /// </summary>
    [Required]
    public required string BaseUrl { get; set; }
}
