using TheLsmArchive.Domain.Models;

namespace TheLsmArchive.Patreon.Ingestion.Models;

/// <summary>
/// Represents a Patreon feed with a title and a list of posts.
/// </summary>
/// <param name="Title">The title of the Patreon feed.</param>
/// <param name="Posts">The list of posts in the Patreon feed.</param>
public record PatreonFeed(string Title, IList<PatreonPost> Posts);
