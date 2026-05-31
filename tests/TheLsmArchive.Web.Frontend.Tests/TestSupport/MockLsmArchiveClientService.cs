using TheLsmArchive.ApiClient;
using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.Web.Frontend.Tests.TestSupport;

/// <summary>
/// A configurable test double for <see cref="ILsmArchiveClientService"/>.
/// </summary>
public sealed class MockLsmArchiveClientService : ILsmArchiveClientService
{
    public Func<SearchRequest, CancellationToken, Task<Result<PagedResponse<SearchResult>>>> SearchHandler { get; set; } =
        (_, _) => Task.FromResult(Result<PagedResponse<SearchResult>>.None());

    public Func<int, CancellationToken, Task<Result<Person>>> GetPersonByIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<Person>.None());

    public Func<int, CancellationToken, Task<Result<List<MostDiscussedTopic>>>> GetMostDiscussedTopicsByPersonIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.None());

    public Func<int, PagedItemRequest, bool, CancellationToken, Task<Result<PagedResponse<Topic>>>> GetTopicsByPersonIdHandler { get; set; } =
        (_, _, _, _) => Task.FromResult(Result<PagedResponse<Topic>>.None());

    public Func<int, PagedItemRequest, bool, CancellationToken, Task<Result<PagedResponse<PersonTimelineEntry>>>> GetEpisodesByPersonIdHandler { get; set; } =
        (_, _, _, _) => Task.FromResult(Result<PagedResponse<PersonTimelineEntry>>.None());

    public Func<int, CancellationToken, Task<Result<Topic>>> GetTopicByIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<Topic>.None());

    public Func<int, PagedItemRequest, bool, CancellationToken, Task<Result<TopicTimeline>>> GetTopicTimelineByIdHandler { get; set; } =
        (_, _, _, _) => Task.FromResult(Result<TopicTimeline>.None());

    public Func<int, CancellationToken, Task<Result<List<MostDiscussedTopic>>>> GetMostDiscussedAlongsideTopicsByTopicIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<List<MostDiscussedTopic>>.None());

    public Func<int, CancellationToken, Task<Result<PersonDetails>>> GetPersonDetailsByIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<PersonDetails>.None());

    public Func<int, CancellationToken, Task<Result<Episode>>> GetLatestEpisodeByPersonIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<Episode>.None());

    public Func<int, CancellationToken, Task<Result<Episode>>> GetEpisodeByIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<Episode>.None());

    public Func<int, CancellationToken, Task<Result<List<Person>>>> GetPersonsByEpisodeIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<List<Person>>.None());

    public Func<int, CancellationToken, Task<Result<List<Topic>>>> GetTopicsByEpisodeIdHandler { get; set; } =
        (_, _) => Task.FromResult(Result<List<Topic>>.None());

    public Func<CancellationToken, Task<Result<List<Episode>>>> GetRecentEpisodesHandler { get; set; } =
        _ => Task.FromResult(Result<List<Episode>>.None());

    public Func<CancellationToken, Task<Result<int>>> GetRandomEpisodeIdHandler { get; set; } =
        _ => Task.FromResult(Result<int>.None());

    public Func<CancellationToken, Task<Result<DateTimeOffset>>> GetLastDataSyncDateTimeAsyncHandler { get; set; } =
        _ => Task.FromResult(Result<DateTimeOffset>.None());

    public Task<Result<PagedResponse<SearchResult>>> Search(SearchRequest request, CancellationToken cancellationToken) =>
        SearchHandler(request, cancellationToken);

    public Task<Result<Person>> GetPersonById(int personId, CancellationToken cancellationToken) =>
        GetPersonByIdHandler(personId, cancellationToken);

    public Task<Result<List<MostDiscussedTopic>>> GetMostDiscussedTopicsByPersonId(int personId, CancellationToken cancellationToken) =>
        GetMostDiscussedTopicsByPersonIdHandler(personId, cancellationToken);

    public Task<Result<PagedResponse<Topic>>> GetTopicsByPersonId(int personId, PagedItemRequest pagedRequest, bool sortDescending, CancellationToken cancellationToken) =>
        GetTopicsByPersonIdHandler(personId, pagedRequest, sortDescending, cancellationToken);

    public Task<Result<PagedResponse<PersonTimelineEntry>>> GetEpisodesByPersonId(int personId, PagedItemRequest pagedRequest, bool sortDescending, CancellationToken cancellationToken) =>
        GetEpisodesByPersonIdHandler(personId, pagedRequest, sortDescending, cancellationToken);

    public Task<Result<Topic>> GetTopicById(int topicId, CancellationToken cancellationToken) =>
        GetTopicByIdHandler(topicId, cancellationToken);

    public Task<Result<TopicTimeline>> GetTopicTimelineById(int topicId, PagedItemRequest pagedRequest, bool sortDescending, CancellationToken cancellationToken) =>
        GetTopicTimelineByIdHandler(topicId, pagedRequest, sortDescending, cancellationToken);

    public Task<Result<List<MostDiscussedTopic>>> GetMostDiscussedAlongsideTopicsByTopicId(int topicId, CancellationToken cancellationToken) =>
        GetMostDiscussedAlongsideTopicsByTopicIdHandler(topicId, cancellationToken);

    public Task<Result<PersonDetails>> GetPersonDetailsById(int personId, CancellationToken cancellationToken) =>
        GetPersonDetailsByIdHandler(personId, cancellationToken);

    public Task<Result<Episode>> GetLatestEpisodeByPersonId(int personId, CancellationToken cancellationToken) =>
        GetLatestEpisodeByPersonIdHandler(personId, cancellationToken);

    public Task<Result<Episode>> GetEpisodeById(int episodeId, CancellationToken cancellationToken) =>
        GetEpisodeByIdHandler(episodeId, cancellationToken);

    public Task<Result<List<Person>>> GetPersonsByEpisodeId(int episodeId, CancellationToken cancellationToken) =>
        GetPersonsByEpisodeIdHandler(episodeId, cancellationToken);

    public Task<Result<List<Topic>>> GetTopicsByEpisodeId(int episodeId, CancellationToken cancellationToken) =>
        GetTopicsByEpisodeIdHandler(episodeId, cancellationToken);

    public Task<Result<List<Episode>>> GetRecentEpisodes(CancellationToken cancellationToken) =>
        GetRecentEpisodesHandler(cancellationToken);

    public Task<Result<int>> GetRandomEpisodeId(CancellationToken cancellationToken) =>
        GetRandomEpisodeIdHandler(cancellationToken);

    public Task<Result<DateTimeOffset>> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken) =>
        GetLastDataSyncDateTimeAsyncHandler(cancellationToken);
}

