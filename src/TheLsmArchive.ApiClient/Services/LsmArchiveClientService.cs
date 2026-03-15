using System.Text.Json;

using Microsoft.Extensions.Logging;

using TheLsmArchive.ApiClient.Services.Abstractions;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.ApiClient.Services;

/// <summary>
/// The LSM Archive API client service.
/// </summary>
public class LsmArchiveClientService : ILsmArchiveClientService
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
        _logger.LogInformation("Searching the LSM Archive with request: {Request}", request);

        using HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            SearchRoute,
            request.ToQueryString());

        return ExecuteRequestAsync<PagedResponse<SearchResult>>(
            requestMessage,
            hasContent: result => result is not null && result.TotalCount > 0,
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

        _logger.LogInformation("Getting person with ID: {PersonId}", personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}");

        return ExecuteRequestAsync<Person>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    public Task<Result<PersonDetails>> GetPersonDetailsById(int personId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);

        _logger.LogInformation("Getting person details with ID: {PersonId}", personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{PersonRoute}/{personId}/details");

        return ExecuteRequestAsync<PersonDetails>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public Task<Result<PagedResponse<Topic>>> GetTopicsByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting topics for person with ID: {PersonId}", personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{PersonRoute}/{personId}/topics",
            pagedRequest.ToQueryString());

        return ExecuteRequestAsync<PagedResponse<Topic>>(
            requestMessage,
            hasContent: result => result is not null && result.Items.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="personId"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagedRequest"/> is null.</exception>
    public Task<Result<PagedResponse<Episode>>> GetEpisodesByPersonId(
        int personId,
        PagedItemRequest pagedRequest,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personId);
        ArgumentNullException.ThrowIfNull(pagedRequest);

        _logger.LogInformation("Getting episodes for person with ID: {PersonId}", personId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{PersonRoute}/{personId}/episodes",
            pagedRequest.ToQueryString());

        return ExecuteRequestAsync<PagedResponse<Episode>>(
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

        _logger.LogInformation("Getting topic with ID: {TopicId}", topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{TopicRoute}/{topicId}");

        return ExecuteRequestAsync<Topic>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    public Task<Result<TopicDetails>> GetTopicDetailsById(
        int topicId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);

        _logger.LogInformation("Getting details for topic with ID: {TopicId}", topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor($"{TopicRoute}/{topicId}/details");

        return ExecuteRequestAsync<TopicDetails>(
            requestMessage,
            hasContent: result => result is not null,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    public Task<Result<List<Episode>>> GetEpisodesByTopicId(int topicId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);

        _logger.LogInformation("Getting episodes for topic with ID: {TopicId}", topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{TopicRoute}/{topicId}/episodes");

        return ExecuteRequestAsync<List<Episode>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="topicId"/> is negative.</exception>
    public Task<Result<List<Person>>> GetPersonsByTopicId(int topicId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(topicId);

        _logger.LogInformation("Getting people for topic with ID: {TopicId}", topicId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{TopicRoute}/{topicId}/people");

        return ExecuteRequestAsync<List<Person>>(
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

        _logger.LogInformation("Getting episode with ID: {EpisodeId}", episodeId);

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

        _logger.LogInformation("Getting people for episode with ID: {EpisodeId}", episodeId);

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

        _logger.LogInformation("Getting topics for episode with ID: {EpisodeId}", episodeId);

        HttpRequestMessage requestMessage = BuildGetRequestMessageFor(
            $"{EpisodesRoute}/{episodeId}/topics");

        return ExecuteRequestAsync<List<Topic>>(
            requestMessage,
            hasContent: result => result is not null && result.Count > 0,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<Result<DateTimeOffset>> GetLastDataSyncDateTimeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting the date and time of the last data synchronization.");

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
                _logger.LogInformation("No content returned for request: {Request}", requestMessage.RequestUri);
                return Result<T>.None();
            }

            T? result = JsonSerializer.Deserialize<T>(content, _jsonOptions);

            if (!hasContent(result))
            {
                _logger.LogInformation("No valid result found for request: {Request}", requestMessage.RequestUri);
                return Result<T>.None();
            }

            return Result<T>.Ok(result!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while executing request: {Request}", requestMessage.RequestUri);
            return Result<T>.Fail(e.Message);
        }
    }
}
