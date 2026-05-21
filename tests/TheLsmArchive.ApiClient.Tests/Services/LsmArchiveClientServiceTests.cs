using System.Net;

using Microsoft.Extensions.Logging.Abstractions;

using TheLsmArchive.ApiClient.Services;
using TheLsmArchive.ApiClient.Tests.Infrastructure;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.ApiClient.Tests.Services;

public class LsmArchiveClientServiceTests
{
    [Fact]
    public void Constructor_WhenHttpClientBaseAddressIsMissing_ThrowsArgumentException()
    {
        // Arrange
        HttpClient httpClient = new(MockHttpClientFactory.CreateHandler((_, _) => MockHttpClientFactory.CreateResponse(HttpStatusCode.OK, string.Empty)));

        // Act
        void Act()
        {
            _ = new LsmArchiveClientService(NullLogger<LsmArchiveClientService>.Instance, httpClient);
        }

        // Assert
        ArgumentException exception = Throws<ArgumentException>(Act);
        Equal("The HTTP client must have a base address.", exception.Message);
    }

    [Fact]
    public async Task Search_WithValidResponse_ReturnsSuccessAndBuildsExpectedRequest()
    {
        // Arrange
        SearchRequest request = new("Sacred Symbols & Friends", EntityType.Person, 2, 25);
        PagedResponse<SearchResult> response = new(
            [new SearchResult(123, "Sacred Symbols", EntityType.Person)],
            1,
            2,
            25);
        HttpRequestMessage? capturedRequest = null;
        CancellationToken capturedCancellationToken = default;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveClientService service = CreateService((requestMessage, ct) =>
        {
            capturedRequest = requestMessage;
            capturedCancellationToken = ct;

            return MockHttpClientFactory.CreateJsonResponse(response);
        });

        // Act
        Result<PagedResponse<SearchResult>> result = await service.Search(request, cancellationToken);

        // Assert
        Result<PagedResponse<SearchResult>>.Success success = IsType<Result<PagedResponse<SearchResult>>.Success>(result);
        Equal(response.TotalCount, success.Data.TotalCount);
        Equal(response.PageNumber, success.Data.PageNumber);
        Equal(response.PageSize, success.Data.PageSize);
        Equal(response.Items, success.Data.Items);
        NotNull(capturedRequest);
        Equal(HttpMethod.Get, capturedRequest.Method);
        True(capturedCancellationToken.CanBeCanceled);
        Equal(
            "https://example.com/api/search?searchTerm=Sacred%20Symbols%20%26%20Friends&entityType=Person&pageNumber=2&pageSize=25",
            capturedRequest.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Search_WithEmptyResponseBody_ReturnsNoContent()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateResponse(HttpStatusCode.OK, string.Empty));

        // Act
        Result<PagedResponse<SearchResult>> result = await service.Search(new SearchRequest("query"), TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<PagedResponse<SearchResult>>.NoContent>(result);
    }

    [Fact]
    public async Task Search_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateResponse(HttpStatusCode.OK, "{invalid-json}"));

        // Act
        Result<PagedResponse<SearchResult>> result = await service.Search(new SearchRequest("query"), TestContext.Current.CancellationToken);

        // Assert
        Result<PagedResponse<SearchResult>>.Failure failure = IsType<Result<PagedResponse<SearchResult>>.Failure>(result);
        False(string.IsNullOrWhiteSpace(failure.Message));
    }

    [Fact]
    public async Task GetPersonById_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        Person response = new(42, "Colin Moriarty");
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<Person> result = await service.GetPersonById(42, TestContext.Current.CancellationToken);

        // Assert
        Result<Person>.Success success = IsType<Result<Person>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/person/42", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetPersonById_WithNullPayload_ReturnsNoContent()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse<Person?>(null));

        // Act
        Result<Person> result = await service.GetPersonById(42, TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<Person>.NoContent>(result);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        List<MostDiscussedTopic> response = [new(5, "Politics", 9)];
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<List<MostDiscussedTopic>> result = await service.GetMostDiscussedTopicsByPersonId(42, TestContext.Current.CancellationToken);

        // Assert
        Result<List<MostDiscussedTopic>>.Success success = IsType<Result<List<MostDiscussedTopic>>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/person/42/topics/most-discussed", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WithEmptyList_ReturnsNoContent()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(new List<MostDiscussedTopic>()));

        // Act
        Result<List<MostDiscussedTopic>> result = await service.GetMostDiscussedTopicsByPersonId(42, TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<List<MostDiscussedTopic>>.NoContent>(result);
    }

    [Fact]
    public async Task GetPersonDetailsById_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        PersonDetails response = new(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<PersonDetails> result = await service.GetPersonDetailsById(42, TestContext.Current.CancellationToken);

        // Assert
        Result<PersonDetails>.Success success = IsType<Result<PersonDetails>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/person/42/details", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetLatestEpisodeByPersonId_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        Episode response = CreateEpisode(77, "Latest Episode");
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<Episode> result = await service.GetLatestEpisodeByPersonId(42, TestContext.Current.CancellationToken);

        // Assert
        Result<Episode>.Success success = IsType<Result<Episode>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/person/42/episodes/latest", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTopicsByPersonId_WithValidResponse_ReturnsSuccessAndBuildsExpectedRequest()
    {
        // Arrange
        PagedItemRequest request = new(3, 10, "Colin Moriarty");
        PagedResponse<Topic> response = new([new Topic(4, "PlayStation")], 1, 3, 10);
        HttpRequestMessage? capturedRequest = null;
        LsmArchiveClientService service = CreateService((requestMessage, _) =>
        {
            capturedRequest = requestMessage;
            return MockHttpClientFactory.CreateJsonResponse(response);
        });

        // Act
        Result<PagedResponse<Topic>> result = await service.GetTopicsByPersonId(42, request, true, TestContext.Current.CancellationToken);

        // Assert
        Result<PagedResponse<Topic>>.Success success = IsType<Result<PagedResponse<Topic>>.Success>(result);
        Equal(response.TotalCount, success.Data.TotalCount);
        Equal(response.PageNumber, success.Data.PageNumber);
        Equal(response.PageSize, success.Data.PageSize);
        Equal(response.Items, success.Data.Items);
        Equal(
            "https://example.com/api/person/42/topics?pageNumber=3&pageSize=10&searchTerm=Colin%20Moriarty&sortDescending=True",
            capturedRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTopicsByPersonId_WithEmptyItems_ReturnsNoContent()
    {
        // Arrange
        PagedResponse<Topic> response = new([], 0, 1, 50);
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(response));

        // Act
        Result<PagedResponse<Topic>> result = await service.GetTopicsByPersonId(42, new PagedItemRequest(), false, TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<PagedResponse<Topic>>.NoContent>(result);
    }

    [Fact]
    public async Task GetEpisodesByPersonId_WithValidResponse_ReturnsSuccessAndBuildsExpectedRequest()
    {
        // Arrange
        PagedItemRequest request = new(2, 5, "Dagan");
        PagedResponse<PersonTimelineEntry> response = new(
            [new PersonTimelineEntry(9, "Timeline Episode", new DateOnly(2024, 5, 1), "https://example.com/posts/9", [new Topic(1, "Publishing")])],
            1,
            2,
            5);
        HttpRequestMessage? capturedRequest = null;
        LsmArchiveClientService service = CreateService((requestMessage, _) =>
        {
            capturedRequest = requestMessage;
            return MockHttpClientFactory.CreateJsonResponse(response);
        });

        // Act
        Result<PagedResponse<PersonTimelineEntry>> result = await service.GetEpisodesByPersonId(42, request, false, TestContext.Current.CancellationToken);

        // Assert
        Result<PagedResponse<PersonTimelineEntry>>.Success success = IsType<Result<PagedResponse<PersonTimelineEntry>>.Success>(result);
        Equal(response.TotalCount, success.Data.TotalCount);
        Equal(response.PageNumber, success.Data.PageNumber);
        Equal(response.PageSize, success.Data.PageSize);
        Single(success.Data.Items);
        PersonTimelineEntry entry = success.Data.Items[0];
        Equal(9, entry.EpisodeId);
        Equal("Timeline Episode", entry.Title);
        Equal(new DateOnly(2024, 5, 1), entry.ReleaseDate);
        Equal("https://example.com/posts/9", entry.PatreonPostLink);
        Equal([new Topic(1, "Publishing")], entry.Topics);
        Equal(
            "https://example.com/api/person/42/episodes?pageNumber=2&pageSize=5&searchTerm=Dagan&sortDescending=False",
            capturedRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTopicById_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        Topic response = new(7, "Games Media");
        HttpRequestMessage? capturedRequest = null;
        LsmArchiveClientService service = CreateService((requestMessage, _) =>
        {
            capturedRequest = requestMessage;
            return MockHttpClientFactory.CreateJsonResponse(response);
        });

        // Act
        Result<Topic> result = await service.GetTopicById(7, TestContext.Current.CancellationToken);

        // Assert
        Result<Topic>.Success success = IsType<Result<Topic>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/topic/7", capturedRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTopicTimelineById_WithValidResponse_ReturnsSuccessAndBuildsExpectedRequest()
    {
        // Arrange
        PagedItemRequest request = new(5, 20, "Patreon");
        TopicTimeline response = new(
            new DateOnly(2021, 1, 1),
            new DateOnly(2024, 12, 31),
            new PagedResponse<TopicTimelineEntry>(
                [new TopicTimelineEntry(7, "Topic Timeline Episode", new DateOnly(2024, 4, 1), "https://example.com/posts/7", [new Person(1, "Chris Ray Gun")])],
                1,
                5,
                20));
        HttpRequestMessage? capturedRequest = null;
        LsmArchiveClientService service = CreateService((requestMessage, _) =>
        {
            capturedRequest = requestMessage;
            return MockHttpClientFactory.CreateJsonResponse(response);
        });

        // Act
        Result<TopicTimeline> result = await service.GetTopicTimelineById(7, request, true, TestContext.Current.CancellationToken);

        // Assert
        Result<TopicTimeline>.Success success = IsType<Result<TopicTimeline>.Success>(result);
        Equal(response.FirstDiscussed, success.Data.FirstDiscussed);
        Equal(response.LastDiscussed, success.Data.LastDiscussed);
        Equal(response.Entries.TotalCount, success.Data.Entries.TotalCount);
        Equal(response.Entries.PageNumber, success.Data.Entries.PageNumber);
        Equal(response.Entries.PageSize, success.Data.Entries.PageSize);
        Single(success.Data.Entries.Items);
        TopicTimelineEntry entry = success.Data.Entries.Items[0];
        Equal(7, entry.EpisodeId);
        Equal("Topic Timeline Episode", entry.Title);
        Equal(new DateOnly(2024, 4, 1), entry.ReleaseDate);
        Equal("https://example.com/posts/7", entry.PatreonPostLink);
        Equal([new Person(1, "Chris Ray Gun")], entry.People);
        Equal(
            "https://example.com/api/topic/7/timeline?pageNumber=5&pageSize=20&searchTerm=Patreon&sortDescending=True",
            capturedRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetMostDiscussedAlongsideTopicsByTopicId_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        List<MostDiscussedTopic> response = [new(8, "Xbox", 3)];
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<List<MostDiscussedTopic>> result = await service.GetMostDiscussedAlongsideTopicsByTopicId(7, TestContext.Current.CancellationToken);

        // Assert
        Result<List<MostDiscussedTopic>>.Success success = IsType<Result<List<MostDiscussedTopic>>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/topic/7/most-discussed-alongside", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetEpisodeById_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        Episode response = CreateEpisode(9, "Episode Nine");
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<Episode> result = await service.GetEpisodeById(9, TestContext.Current.CancellationToken);

        // Assert
        Result<Episode>.Success success = IsType<Result<Episode>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/episode/9", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetEpisodeById_WhenApiReturnsNonSuccessStatus_ReturnsFailure()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateResponse(HttpStatusCode.InternalServerError, "boom", "text/plain"));

        // Act
        Result<Episode> result = await service.GetEpisodeById(9, TestContext.Current.CancellationToken);

        // Assert
        Result<Episode>.Failure failure = IsType<Result<Episode>.Failure>(result);
        False(string.IsNullOrWhiteSpace(failure.Message));
    }

    [Fact]
    public async Task GetPersonsByEpisodeId_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        List<Person> response = [new Person(1, "Dustin Furman")];
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<List<Person>> result = await service.GetPersonsByEpisodeId(9, TestContext.Current.CancellationToken);

        // Assert
        Result<List<Person>>.Success success = IsType<Result<List<Person>>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/episode/9/people", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTopicsByEpisodeId_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        List<Topic> response = [new Topic(2, "Nintendo")];
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<List<Topic>> result = await service.GetTopicsByEpisodeId(9, TestContext.Current.CancellationToken);

        // Assert
        Result<List<Topic>>.Success success = IsType<Result<List<Topic>>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/episode/9/topics", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetRecentEpisodes_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        List<Episode> response = [CreateEpisode(1, "Recent Episode")];
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<List<Episode>> result = await service.GetRecentEpisodes(TestContext.Current.CancellationToken);

        // Assert
        Result<List<Episode>>.Success success = IsType<Result<List<Episode>>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/episode/recent", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WithPositiveId_ReturnsSuccess()
    {
        // Arrange
        LsmArchiveClientService service = CreateJsonService(99, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<int> result = await service.GetRandomEpisodeId(TestContext.Current.CancellationToken);

        // Assert
        Result<int>.Success success = IsType<Result<int>.Success>(result);
        Equal(99, success.Data);
        Equal("https://example.com/api/episode/random", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WithZero_ReturnsNoContent()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(0));

        // Act
        Result<int> result = await service.GetRandomEpisodeId(TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<int>.NoContent>(result);
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WithValidTimestamp_ReturnsSuccess()
    {
        // Arrange
        DateTimeOffset response = new(2026, 5, 21, 14, 30, 0, TimeSpan.Zero);
        LsmArchiveClientService service = CreateJsonService(response, out Func<HttpRequestMessage?> getCapturedRequest);

        // Act
        Result<DateTimeOffset> result = await service.GetLastDataSyncDateTimeAsync(TestContext.Current.CancellationToken);

        // Assert
        Result<DateTimeOffset>.Success success = IsType<Result<DateTimeOffset>.Success>(result);
        Equal(response, success.Data);
        Equal("https://example.com/api/system/last-data-sync", getCapturedRequest()!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WithDefaultTimestamp_ReturnsNoContent()
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(default(DateTimeOffset)));

        // Act
        Result<DateTimeOffset> result = await service.GetLastDataSyncDateTimeAsync(TestContext.Current.CancellationToken);

        // Assert
        IsType<Result<DateTimeOffset>.NoContent>(result);
    }

    [Theory]
    [MemberData(nameof(NegativePersonIdActions))]
    public async Task PersonMethods_WithNegativeId_ThrowArgumentOutOfRangeException(
        Func<LsmArchiveClientService, CancellationToken, Task> action)
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(new Person(1, "Name")));

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => action(service, TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(NullPagedRequestActions))]
    public async Task PagedMethods_WithNullPagedRequest_ThrowArgumentNullException(
        Func<LsmArchiveClientService, CancellationToken, Task> action)
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(new object()));

        // Act & Assert
        await ThrowsAsync<ArgumentNullException>(() => action(service, TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(NegativeTopicIdActions))]
    public async Task TopicMethods_WithNegativeId_ThrowArgumentOutOfRangeException(
        Func<LsmArchiveClientService, CancellationToken, Task> action)
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(new Topic(1, "Topic")));

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => action(service, TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(NegativeEpisodeIdActions))]
    public async Task EpisodeMethods_WithNegativeId_ThrowArgumentOutOfRangeException(
        Func<LsmArchiveClientService, CancellationToken, Task> action)
    {
        // Arrange
        LsmArchiveClientService service = CreateService((_, _) => MockHttpClientFactory.CreateJsonResponse(CreateEpisode(1, "Episode")));

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => action(service, TestContext.Current.CancellationToken));
    }

    public static TheoryData<Func<LsmArchiveClientService, CancellationToken, Task>> NegativePersonIdActions =>
        new()
        {
            async (service, cancellationToken) => await service.GetPersonById(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetMostDiscussedTopicsByPersonId(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetPersonDetailsById(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetLatestEpisodeByPersonId(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetTopicsByPersonId(-1, new PagedItemRequest(), false, cancellationToken),
            async (service, cancellationToken) => await service.GetEpisodesByPersonId(-1, new PagedItemRequest(), true, cancellationToken)
        };

    public static TheoryData<Func<LsmArchiveClientService, CancellationToken, Task>> NullPagedRequestActions =>
        new()
        {
            async (service, cancellationToken) => await service.GetTopicsByPersonId(1, null!, true, cancellationToken),
            async (service, cancellationToken) => await service.GetEpisodesByPersonId(1, null!, false, cancellationToken),
            async (service, cancellationToken) => await service.GetTopicTimelineById(1, null!, true, cancellationToken)
        };

    public static TheoryData<Func<LsmArchiveClientService, CancellationToken, Task>> NegativeTopicIdActions =>
        new()
        {
            async (service, cancellationToken) => await service.GetTopicById(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetTopicTimelineById(-1, new PagedItemRequest(), false, cancellationToken),
            async (service, cancellationToken) => await service.GetMostDiscussedAlongsideTopicsByTopicId(-1, cancellationToken)
        };

    public static TheoryData<Func<LsmArchiveClientService, CancellationToken, Task>> NegativeEpisodeIdActions =>
        new()
        {
            async (service, cancellationToken) => await service.GetEpisodeById(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetPersonsByEpisodeId(-1, cancellationToken),
            async (service, cancellationToken) => await service.GetTopicsByEpisodeId(-1, cancellationToken)
        };

    private static LsmArchiveClientService CreateService(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory,
        string baseAddress = "https://example.com/api/")
    {
        HttpClient httpClient = MockHttpClientFactory.CreateClient(responseFactory, baseAddress);

        return new LsmArchiveClientService(NullLogger<LsmArchiveClientService>.Instance, httpClient);
    }

    private static LsmArchiveClientService CreateJsonService<T>(
        T response,
        out Func<HttpRequestMessage?> getCapturedRequest,
        string baseAddress = "https://example.com/api/")
    {
        HttpRequestMessage? capturedRequest = null;
        getCapturedRequest = () => capturedRequest;

        return CreateService((requestMessage, _) =>
        {
            capturedRequest = requestMessage;
            return MockHttpClientFactory.CreateJsonResponse(response);
        }, baseAddress);
    }


    private static Episode CreateEpisode(int id, string title) =>
        new(id, title, new DateOnly(2024, 1, 1), $"https://example.com/posts/{id}", $"<p>{title}</p>");
}
