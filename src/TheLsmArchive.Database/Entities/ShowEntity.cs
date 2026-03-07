using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Database.Entities;

/// <summary>
/// The show entity.
/// </summary>
public class ShowEntity : BaseEntity
{
    /// <summary>
    /// The name of the show.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The timestamp of the last sync for this show.
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>
    /// The Patreon posts for the show.
    /// </summary>
    public ICollection<PatreonPostEntity> PatreonPosts { get; set; } = [];

    /// <summary>
    /// The episodes of the show.
    /// </summary>
    public ICollection<EpisodeEntity> Episodes { get; set; } = [];
}
