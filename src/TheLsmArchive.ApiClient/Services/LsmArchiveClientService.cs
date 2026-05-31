using System.Text.Json;

using Microsoft.Extensions.Logging;

using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.ApiClient.Services;

/// <summary>
/// The LSM Archive API client service.
/// </summary>
public partial class LsmArchiveClientService : ILsmArchiveClientService
{
    private const string SearchRoute = "search";

    private const string PersonRoute = "person";

    private const string TopicRoute = "topic";

    private const string EpisodesRoute = "episode";

    private const string SystemRoute = "system";

    private readonly ILogger<LsmArchiveClientService> _logger;

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="LsmArchiveClientService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    public LsmArchiveClientService(
        ILogger<LsmArchiveClientService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;

        if (httpClient.BaseAddress is null)
        {
            throw new ArgumentException("The HTTP client must have a base address.");
        }
    }

    /// <inheritdoc />
    public Task<Result<PagedResponse<SearchResult>>> Search(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        LogSearchingArchive(request);

        using HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            SearchRoute,
            request.ToQueryString());

        return ExecuteRequestAsync<PagedResponse<SearchResult>>(
            requestMessage,
            hasContent: result => result is { Items.Count: > 0 },
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    public Task<Result<Person>> GetPersonById(
        int personId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);

        LogGettingPersonById(personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}");

        return ExecuteRequestAsync<Person>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    public Task<Result<List<MostDiscussedTopic>>> GetMostDiscussedTopicsByPersonId(
        int personId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);

        LogGettingMostDiscussedTopicsByPerson(personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}/topics/most-discussed");

        return ExecuteRequestAsync<List<MostDiscussedTopic>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    public Task<Result<PersonDetails>> GetPersonDetailsById(int personId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);

        LogGettingPersonDetailsById(personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}/details");

        return ExecuteRequestAsync<PersonDetails>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    public Task<Result<Episode>> GetLatestEpisodeByPersonId(
        int personId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);
        LogGettingLatestEpisodeByPerson(personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}/episodes/latest");

        return ExecuteRequestAsync<Episode>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public Task<Result<PagedResponse<Topic>>> GetTopicsByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        LogGettingTopicsByPerson(personId);

        string queryString = $"{pagedRequest.ToQueryString()}&sortDescending={sortDescending}";

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{PersonRoute}/{personId}/topics",
            queryString);

        return ExecuteRequestAsync<PagedResponse<Topic>>(
            requestMessage,
            hasContent: result => result is not null && result.Items.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public Task<Result<PagedResponse<PersonTimelineEntry>>> GetEpisodesByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        LogGettingEpisodesByPerson(personId);

        string queryString = $"{pagedRequest.ToQueryString()}&sortDescending={sortDescending}";

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{PersonRoute}/{personId}/episodes",
            queryString);

        return ExecuteRequestAsync<PagedResponse<PersonTimelineEntry>>(
            requestMessage,
            hasContent: result => result is not null && result.Items.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    public Task<Result<Topic>> GetTopicById(
        int topicId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);

        LogGettingTopicById(topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{TopicRoute}/{topicId}");

        return ExecuteRequestAsync<Topic>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public Task<Result<TopicTimeline>> GetTopicTimelineById(
        int topicId,
        PagedItemRequest pagedRequest,
        bool sortDescending,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        LogGettingTopicTimelineById(topicId);

        string queryString = $"{pagedRequest.ToQueryString()}&sortDescending={sortDescending}";
        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{TopicRoute}/{topicId}/timeline",
            queryString);

        return ExecuteRequestAsync<TopicTimeline>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    public Task<Result<List<MostDiscussedTopic>>> GetMostDiscussedAlongsideTopicsByTopicId(
        int topicId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);

        LogGettingMostDiscussedAlongsideTopicsByTopic(topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{TopicRoute}/{topicId}/most-discussed-alongside");

        return ExecuteRequestAsync<List<MostDiscussedTopic>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="episodeId"/> is negative.</exception>
    public Task<Result<Episode>> GetEpisodeById(
        int episodeId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(episodeId);

        LogGettingEpisodeById(episodeId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{EpisodesRoute}/{episodeId}");

        return ExecuteRequestAsync<Episode>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="episodeId"/> is negative.</exception>
    public Task<Result<List<Person>>> GetPersonsByEpisodeId(int episodeId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(episodeId);

        LogGettingPersonsByEpisode(episodeId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{EpisodesRoute}/{episodeId}/people");

        return ExecuteRequestAsync<List<Person>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="episodeId"/> is negative.</exception>
    public Task<Result<List<Topic>>> GetTopicsByEpisodeId(int episodeId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(episodeId);

        LogGettingTopicsByEpisode(episodeId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{EpisodesRoute}/{episodeId}/topics");

        return ExecuteRequestAsync<List<Topic>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<Result<List<Episode>>> GetRecentEpisodes(CancellationToken cancellationToken)
    {
        LogGettingRecentEpisodes();

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{EpisodesRoute}/recent");

        return ExecuteRequestAsync<List<Episode>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<Result<int>> GetRandomEpisodeId(CancellationToken cancellationToken)
    {
        LogGettingRandomEpisodeId();

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{EpisodesRoute}/random");

        return ExecuteRequestAsync<int>(
            requestMessage,
            hasContent: result => result is > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<Result<DateTimeOffset>> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken)
    {
        LogGettingLastDataSync();

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{SystemRoute}/last-data-sync");

        return ExecuteRequestAsync<DateTimeOffset>(
            requestMessage,
            hasContent: result => result != default,
            cancellationToken
        );
    }

    private HttpRequestMessage BuildGetRequestMessageFor(
        string route,
        string? query = null)
    {
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, route))
        {
            Query = query
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

        return requestMessage;
    }

    private async Task<Result<T>> ExecuteRequestAsync<T>(
        HttpRequestMessage requestMessage,
        Func<T?, bool> hasContent,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage responseMessage = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken);

            responseMessage.EnsureSuccessStatusCode();

            string content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                LogNoContentReturned(requestMessage.RequestUri);
                return Result<T>.None();
            }

            T? result = JsonSerializer.Deserialize<T>(content, _jsonOptions);

            if (!hasContent(result))
            {
                LogNoValidResultFound(requestMessage.RequestUri);
                return Result<T>.None();
            }

            return Result<T>.Ok(result!);
        }
        catch (Exception e)
        {
            LogRequestFailed(e, requestMessage.RequestUri);
            return Result<T>.Fail(e.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching the LSM Archive with request: {Request}")]
    private partial void LogSearchingArchive(SearchRequest request);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting person with ID: {PersonId}")]
    private partial void LogGettingPersonById(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting most discussed topics for person with ID: {PersonId}")]
    private partial void LogGettingMostDiscussedTopicsByPerson(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting person details with ID: {PersonId}")]
    private partial void LogGettingPersonDetailsById(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting latest episode for person with ID: {PersonId}")]
    private partial void LogGettingLatestEpisodeByPerson(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting topics for person with ID: {PersonId}")]
    private partial void LogGettingTopicsByPerson(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting episodes for person with ID: {PersonId}")]
    private partial void LogGettingEpisodesByPerson(int personId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting topic with ID: {TopicId}")]
    private partial void LogGettingTopicById(int topicId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting timeline for topic with ID: {TopicId}")]
    private partial void LogGettingTopicTimelineById(int topicId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting most discussed alongside topics for topic with ID: {TopicId}")]
    private partial void LogGettingMostDiscussedAlongsideTopicsByTopic(int topicId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting episode with ID: {EpisodeId}")]
    private partial void LogGettingEpisodeById(int episodeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting people for episode with ID: {EpisodeId}")]
    private partial void LogGettingPersonsByEpisode(int episodeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting topics for episode with ID: {EpisodeId}")]
    private partial void LogGettingTopicsByEpisode(int episodeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting the most recent episodes from the last 7 days.")]
    private partial void LogGettingRecentEpisodes();

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting a random episode ID.")]
    private partial void LogGettingRandomEpisodeId();

    [LoggerMessage(Level = LogLevel.Information, Message = "Getting the date and time of the last data synchronization.")]
    private partial void LogGettingLastDataSync();

    [LoggerMessage(Level = LogLevel.Information, Message = "No content returned for request: {Request}")]
    private partial void LogNoContentReturned(Uri? request);

    [LoggerMessage(Level = LogLevel.Information, Message = "No valid result found for request: {Request}")]
    private partial void LogNoValidResultFound(Uri? request);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while executing request: {Request}")]
    private partial void LogRequestFailed(Exception exception, Uri? request);
}
