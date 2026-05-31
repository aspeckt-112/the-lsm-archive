namespace TheLsmArchive.Models.Response;

/// <summary>
/// Represents the timeline of a topic, including all episodes where the topic was discussed.
/// </summary>
/// <param name="FirstDiscussed">The date the topic was first discussed.</param>
/// <param name="LastDiscussed">The date the topic was most recently discussed.</param>
/// <param name="Entries">The paginated timeline entries, ordered by release date descending.</param>
public record TopicTimeline(
    DateOnly FirstDiscussed,
    DateOnly LastDiscussed,
    PagedResponse<TopicTimelineEntry> Entries);
