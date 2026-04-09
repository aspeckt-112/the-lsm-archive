namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents a single entry in a person's episode timeline.
/// </summary>
/// <param name="EpisodeId">The unique identifier of the episode.</param>
/// <param name="Title">The title of the episode.</param>
/// <param name="ReleaseDate">The release date of the episode.</param>
/// <param name="PatreonPostLink">The link to the Patreon post for the episode.</param>
/// <param name="Topics">The topics discussed in this episode.</param>
public record PersonTimelineEntry(
    int EpisodeId,
    string Title,
    DateOnly ReleaseDate,
    string PatreonPostLink,
    List<Topic> Topics);
