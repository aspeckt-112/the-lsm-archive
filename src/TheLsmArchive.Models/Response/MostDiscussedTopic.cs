namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents a topic ranked by how often it appears alongside a person in episodes.
/// </summary>
/// <param name="Id">The topic ID.</param>
/// <param name="Name">The topic name.</param>
/// <param name="EpisodeCount">The number of episodes where the person and topic co-occur.</param>
public record MostDiscussedTopic(
    int Id,
    string Name,
    int EpisodeCount);
