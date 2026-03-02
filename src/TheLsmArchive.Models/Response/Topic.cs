namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents a topic.
/// </summary>
/// <param name="Id">The topic ID.</param>
/// <param name="Name">The topic name.</param>
public record Topic(
    int Id,
    string Name);
