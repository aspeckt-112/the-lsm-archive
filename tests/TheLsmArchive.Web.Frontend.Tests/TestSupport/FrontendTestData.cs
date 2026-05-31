using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.Web.Frontend.Tests.TestSupport;

/// <summary>
/// Provides shared frontend test models.
/// </summary>
internal static class FrontendTestData
{
    internal static Episode CreateEpisode(
        int id = 1,
        string title = "Sacred Symbols 300",
        DateOnly? releaseDate = null,
        string? patreonPostLink = null,
        string? summaryHtml = null) =>
        new(
            id,
            title,
            releaseDate ?? new DateOnly(2026, 5, 1),
            patreonPostLink ?? $"https://example.com/posts/{id}",
            summaryHtml ?? $"<p>{title}</p>");

    internal static Person CreatePerson(int id = 1, string name = "Colin Moriarty") => new(id, name);

    internal static Topic CreateTopic(int id = 1, string name = "PlayStation") => new(id, name);

    internal static SearchResult CreateSearchResult(int id = 1, string matched = "Colin Moriarty", EntityType entityType = EntityType.Person) =>
        new(id, matched, entityType);

    internal static MostDiscussedTopic CreateMostDiscussedTopic(int id = 1, string name = "PlayStation", int episodeCount = 3) =>
        new(id, name, episodeCount);

    internal static PersonDetails CreatePersonDetails(
        DateOnly? firstAppeared = null,
        DateOnly? lastAppeared = null) =>
        new(firstAppeared ?? new DateOnly(2020, 1, 1), lastAppeared ?? new DateOnly(2026, 5, 1));

    internal static PersonTimelineEntry CreatePersonTimelineEntry(
        int episodeId = 1,
        string title = "Timeline Episode",
        DateOnly? releaseDate = null,
        string? patreonPostLink = null,
        List<Topic>? topics = null) =>
        new(
            episodeId,
            title,
            releaseDate ?? new DateOnly(2026, 5, 1),
            patreonPostLink ?? $"https://example.com/posts/{episodeId}",
            topics ?? [CreateTopic()]);

    internal static TopicTimelineEntry CreateTopicTimelineEntry(
        int episodeId = 1,
        string title = "Topic Timeline Episode",
        DateOnly? releaseDate = null,
        string? patreonPostLink = null,
        List<Person>? people = null) =>
        new(
            episodeId,
            title,
            releaseDate ?? new DateOnly(2026, 5, 1),
            patreonPostLink ?? $"https://example.com/posts/{episodeId}",
            people ?? [CreatePerson()]);

    internal static TopicTimeline CreateTopicTimeline(
        DateOnly? firstDiscussed = null,
        DateOnly? lastDiscussed = null,
        PagedResponse<TopicTimelineEntry>? entries = null) =>
        new(
            firstDiscussed ?? new DateOnly(2020, 1, 1),
            lastDiscussed ?? new DateOnly(2026, 5, 1),
            entries ?? new PagedResponse<TopicTimelineEntry>([CreateTopicTimelineEntry()], 1, 1, 25));
}

