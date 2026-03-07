using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

/// <summary>
/// The Patreon post entity.
/// </summary>
public class PatreonPostEntity : BaseEntity
{
    /// <summary>
    /// The show ID.
    /// </summary>
    public int ShowId { get; set; }

    /// <summary>
    /// The show.
    /// </summary>
    public ShowEntity Show { get; set; } = null!;

    /// <summary>
    /// The Patreon post ID.
    /// </summary>
    public int PatreonId { get; set; }

    /// <summary>
    /// The title of the Patreon post.
    /// </summary>
    /// <remarks>
    /// Can be assumed to be the title of the episode.
    /// </remarks>
    public string Title { get; set; } = null!;

    /// <summary>
    /// The published date of the Patreon post.
    /// </summary>
    public DateTimeOffset Published { get; set; }

    /// <summary>
    /// The summary of the Patreon post.
    /// </summary>
    /// <remarks>
    /// Often includes HTML content.
    /// </remarks>
    public string Summary { get; set; } = null!;

    /// <summary>
    /// The link to the Patreon post.
    /// </summary>
    public string Link { get; set; } = null!;

    /// <summary>
    /// The audio URL of the Patreon post.
    /// </summary>
    public string AudioUrl { get; set; } = null!;

    /// <summary>
    /// The processing error message, if any.
    /// </summary>
    /// <remarks>
    /// Null if processing succeeded or has not been attempted.
    /// Posts with errors will be retried on subsequent runs.
    /// </remarks>
    public string? ProcessingError { get; set; }

    /// <summary>
    /// The associated episode ID, if any.
    /// </summary>
    public int? EpisodeId { get; set; }

    /// <summary>
    /// The associated episode, if any.
    /// </summary>
    public EpisodeEntity? Episode { get; set; }
}
