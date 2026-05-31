namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents a single entry in a topic's timeline.
/// </summary>
/// <param name="EpisodeId">The unique identifier of the episode.</param>
/// <param name="Title">The title of the episode.</param>
/// <param name="ReleaseDate">The release date of the episode.</param>
/// <param name="PatreonPostLink">The link to the Patreon post for the episode.</param>
/// <param name="People">The people who appeared in this episode.</param>
public record TopicTimelineEntry(
    int EpisodeId,
    string Title,
    DateOnly ReleaseDate,
    string PatreonPostLink,
    List<Person> People);
