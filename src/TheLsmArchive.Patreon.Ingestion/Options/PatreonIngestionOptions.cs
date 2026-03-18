using System.ComponentModel.DataAnnotations;

namespace TheLsmArchive.Patreon.Ingestion.Options;

/// <summary>
/// Configuration options for the Patreon ingestion background service.
/// </summary>
public record PatreonIngestionOptions
{
    /// <summary>
    /// Gets the delay in minutes between ingestion cycles.
    /// </summary>
    [Range(1, 1_440)]
    public int RefreshIntervalInMinutes { get; init; } = 60;
}
