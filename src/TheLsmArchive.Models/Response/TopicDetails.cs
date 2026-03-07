namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents detailed information about a topic.
/// </summary>
/// <param name="FirstDiscussed">The date the topic was first discussed, if available.</param>
/// <param name="LastDiscussed">The date the topic was most recently discussed, if available.</param>
public record TopicDetails(
    DateOnly FirstDiscussed,
    DateOnly LastDiscussed);
