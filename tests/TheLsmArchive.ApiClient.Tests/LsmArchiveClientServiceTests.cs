using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.ApiClient.Services;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;

namespace TheLsmArchive.ApiClient.Tests;

public class LsmArchiveClientServiceTests
{
    private readonly Mock<ILogger<LsmArchiveClientService>> _loggerMock = new();

    private LsmArchiveClientService CreateService(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com/") };
        return new LsmArchiveClientService(_loggerMock.Object, httpClient);
    }

    [Fact]
    public void Constructor_ThrowsWhenBaseAddressIsNull()
    {
        HttpClient httpClient = new();

        Assert.Throws<ArgumentException>(
            () => new LsmArchiveClientService(_loggerMock.Object, httpClient));
    }

    [Fact]
    public async Task GetPersonById_WithNegativeId_Throws()
    {
        using MockHandler handler = new("""{"id":1,"name":"Test"}""");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetPersonById(-1, CancellationToken.None));
    }

    [Fact]
    public async Task GetPersonById_WithValidResponse_ReturnsSuccess()
    {
        string json = JsonSerializer.Serialize(new { id = 1, name = "Colin Moriarty" });
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<Person> result = await service.GetPersonById(1, CancellationToken.None);

        Result<Person>.Success success = Assert.IsType<Result<Person>.Success>(result);
        Assert.Equal(1, success.Data.Id);
        Assert.Equal("Colin Moriarty", success.Data.Name);
    }

    [Fact]
    public async Task GetPersonById_WithEmptyResponse_ReturnsNoContent()
    {
        using MockHandler handler = new("");
        LsmArchiveClientService service = CreateService(handler);

        Result<Person> result = await service.GetPersonById(1, CancellationToken.None);

        Assert.IsType<Result<Person>.NoContent>(result);
    }

    [Fact]
    public async Task GetPersonById_With404_ReturnsFailure()
    {
        using MockHandler handler = new("", HttpStatusCode.NotFound);
        LsmArchiveClientService service = CreateService(handler);

        Result<Person> result = await service.GetPersonById(1, CancellationToken.None);

        Assert.IsType<Result<Person>.Failure>(result);
    }

    [Fact]
    public async Task GetEpisodeById_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetEpisodeById(-5, CancellationToken.None));
    }

    [Fact]
    public async Task GetTopicById_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetTopicById(-1, CancellationToken.None));
    }

    [Fact]
    public async Task GetTopicTimelineById_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetTopicTimelineById(-1, new PagedItemRequest(), true, CancellationToken.None));
    }

    [Fact]
    public async Task GetTopicTimelineById_WithNullRequest_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetTopicTimelineById(1, null!, true, CancellationToken.None));
    }

    [Fact]
    public async Task GetTopicTimelineById_WithValidResponse_ReturnsSuccess()
    {
        var timeline = new
        {
            firstDiscussed = "2024-01-01",
            lastDiscussed = "2024-06-01",
            entries = new
            {
                items = new[]
                {
                    new { episodeId = 1, title = "Episode A", releaseDate = "2024-01-01", patreonPostLink = "https://patreon.com/1", people = new[] { new { id = 1, name = "Person A" } } }
                },
                totalCount = 1,
                pageNumber = 1,
                pageSize = 25
            }
        };
        string json = JsonSerializer.Serialize(timeline);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<TopicTimeline> result = await service.GetTopicTimelineById(1, new PagedItemRequest(), true, CancellationToken.None);

        Result<TopicTimeline>.Success success = Assert.IsType<Result<TopicTimeline>.Success>(result);
        Assert.Single(success.Data.Entries.Items);
        Assert.Equal("Episode A", success.Data.Entries.Items[0].Title);
        Assert.Single(success.Data.Entries.Items[0].People);
    }

    [Fact]
    public async Task Search_WithResults_ReturnsSuccess()
    {
        var pagedResponse = new
        {
            items = new[] { new { id = 1, matched = "PlayStation", entityType = 2 } },
            totalCount = 1,
            pageNumber = 1,
            pageSize = 50
        };
        string json = JsonSerializer.Serialize(pagedResponse);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        SearchRequest request = new("PlayStation", EntityType.Topic);
        Result<PagedResponse<SearchResult>> result = await service.Search(request, CancellationToken.None);

        Result<PagedResponse<SearchResult>>.Success success =
            Assert.IsType<Result<PagedResponse<SearchResult>>.Success>(result);
        Assert.Single(success.Data.Items);
    }

    [Fact]
    public async Task Search_WithEmptyItems_ReturnsNoContent()
    {
        var pagedResponse = new
        {
            items = Array.Empty<object>(),
            totalCount = 0,
            pageNumber = 1,
            pageSize = 50
        };
        string json = JsonSerializer.Serialize(pagedResponse);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        SearchRequest request = new("nothing", EntityType.All);
        Result<PagedResponse<SearchResult>> result = await service.Search(request, CancellationToken.None);

        Assert.IsType<Result<PagedResponse<SearchResult>>.NoContent>(result);
    }

    [Fact]
    public async Task GetRecentEpisodes_WithResults_ReturnsSuccess()
    {
        var episodes = new[]
        {
            new { id = 1, title = "Ep 1", releaseDate = "2024-01-15", patreonPostLink = "https://patreon.com/1", summaryHtml = "" }
        };
        string json = JsonSerializer.Serialize(episodes);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<List<Episode>> result = await service.GetRecentEpisodes(CancellationToken.None);

        Assert.IsType<Result<List<Episode>>.Success>(result);
    }

    [Fact]
    public async Task GetRecentEpisodes_WithEmptyList_ReturnsNoContent()
    {
        string json = JsonSerializer.Serialize(Array.Empty<object>());
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<List<Episode>> result = await service.GetRecentEpisodes(CancellationToken.None);

        Assert.IsType<Result<List<Episode>>.NoContent>(result);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WithPositiveId_ReturnsSuccess()
    {
        string json = JsonSerializer.Serialize(42);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<int> result = await service.GetRandomEpisodeId(CancellationToken.None);

        Result<int>.Success success = Assert.IsType<Result<int>.Success>(result);
        Assert.Equal(42, success.Data);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WithZero_ReturnsNoContent()
    {
        string json = JsonSerializer.Serialize(0);
        using MockHandler handler = new(json);
        LsmArchiveClientService service = CreateService(handler);

        Result<int> result = await service.GetRandomEpisodeId(CancellationToken.None);

        Assert.IsType<Result<int>.NoContent>(result);
    }

    [Fact]
    public async Task GetTopicsByPersonId_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetTopicsByPersonId(-1, new PagedItemRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GetTopicsByPersonId_WithNullRequest_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetTopicsByPersonId(1, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetMostDiscussedTopicsByPersonId(-1, CancellationToken.None));
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WithResults_ReturnsSuccess()
    {
        var response = new[]
        {
            new { id = 1, name = "Topic A", episodeCount = 3 }
        };
        using MockHandler handler = new(JsonSerializer.Serialize(response));
        LsmArchiveClientService service = CreateService(handler);

        Result<List<MostDiscussedTopic>> result = await service.GetMostDiscussedTopicsByPersonId(1, CancellationToken.None);

        Result<List<MostDiscussedTopic>>.Success success = Assert.IsType<Result<List<MostDiscussedTopic>>.Success>(result);
        Assert.Single(success.Data);
        Assert.Equal(3, success.Data[0].EpisodeCount);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WithEmptyItems_ReturnsNoContent()
    {
        using MockHandler handler = new(JsonSerializer.Serialize(Array.Empty<object>()));
        LsmArchiveClientService service = CreateService(handler);

        Result<List<MostDiscussedTopic>> result = await service.GetMostDiscussedTopicsByPersonId(1, CancellationToken.None);

        Assert.IsType<Result<List<MostDiscussedTopic>>.NoContent>(result);
    }

    [Fact]
    public async Task GetEpisodesByPersonId_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetEpisodesByPersonId(-1, new PagedItemRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GetEpisodesByPersonId_WithNullRequest_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetEpisodesByPersonId(1, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetEpisodesByTopicId_WithNegativeId_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetEpisodesByTopicId(-1, new PagedItemRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GetEpisodesByTopicId_WithNullRequest_Throws()
    {
        using MockHandler handler = new("{}");
        LsmArchiveClientService service = CreateService(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetEpisodesByTopicId(1, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetEpisodesByTopicId_WithResults_ReturnsSuccess()
    {
        var pagedResponse = new
        {
            items = new[] { new { id = 1, title = "Episode A", releaseDate = "2024-01-15", patreonPostLink = "https://patreon.com/1", summaryHtml = "Summary" } },
            totalCount = 1,
            pageNumber = 1,
            pageSize = 50
        };
        using MockHandler handler = new(JsonSerializer.Serialize(pagedResponse));
        LsmArchiveClientService service = CreateService(handler);

        Result<PagedResponse<Episode>> result = await service.GetEpisodesByTopicId(1, new PagedItemRequest(), CancellationToken.None);

        Result<PagedResponse<Episode>>.Success success = Assert.IsType<Result<PagedResponse<Episode>>.Success>(result);
        Assert.Single(success.Data.Items);
    }

    [Fact]
    public async Task GetEpisodesByTopicId_WithEmptyItems_ReturnsNoContent()
    {
        var pagedResponse = new
        {
            items = Array.Empty<object>(),
            totalCount = 0,
            pageNumber = 1,
            pageSize = 50
        };
        using MockHandler handler = new(JsonSerializer.Serialize(pagedResponse));
        LsmArchiveClientService service = CreateService(handler);

        Result<PagedResponse<Episode>> result = await service.GetEpisodesByTopicId(1, new PagedItemRequest(), CancellationToken.None);

        Assert.IsType<Result<PagedResponse<Episode>>.NoContent>(result);
    }

    [Fact]
    public async Task ExecuteRequest_SetsCorrectRequestUri()
    {
        string json = JsonSerializer.Serialize(new { id = 5, name = "Test" });
        using CapturingHandler handler = new(json);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com/") };
        LsmArchiveClientService service = new(_loggerMock.Object, httpClient);

        await service.GetPersonById(5, CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.test.com/person/5", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task Search_SetsCorrectQueryString()
    {
        var response = new { items = new[] { new { id = 1, matched = "x", entityType = 0 } }, totalCount = 1, pageNumber = 1, pageSize = 50 };
        using CapturingHandler handler = new(JsonSerializer.Serialize(response));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com/") };
        LsmArchiveClientService service = new(_loggerMock.Object, httpClient);

        await service.Search(new SearchRequest("dark souls", EntityType.Episode, 2, 10), CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        string uri = handler.LastRequest.RequestUri!.ToString();
        Assert.Contains("searchTerm=dark", uri);
        Assert.Contains("souls", uri);
        Assert.Contains("entityType=Episode", uri);
        Assert.Contains("pageNumber=2", uri);
        Assert.Contains("pageSize=10", uri);
    }

    [Fact]
    public async Task GetEpisodesByTopicId_SetsCorrectQueryString()
    {
        var response = new
        {
            items = new[] { new { id = 1, title = "Episode A", releaseDate = "2024-01-15", patreonPostLink = "https://patreon.com/1", summaryHtml = "Summary" } },
            totalCount = 1,
            pageNumber = 2,
            pageSize = 10
        };
        using CapturingHandler handler = new(JsonSerializer.Serialize(response));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com/") };
        LsmArchiveClientService service = new(_loggerMock.Object, httpClient);

        await service.GetEpisodesByTopicId(7, new PagedItemRequest(2, 10, "gaming"), CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        string uri = handler.LastRequest.RequestUri!.ToString();
        Assert.Equal("https://api.test.com/topic/7/episodes?pageNumber=2&pageSize=10&searchTerm=gaming", uri);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_SetsCorrectRequestUri()
    {
        var response = new[]
        {
            new { id = 1, name = "Topic A", episodeCount = 2 }
        };
        using CapturingHandler handler = new(JsonSerializer.Serialize(response));
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://api.test.com/") };
        LsmArchiveClientService service = new(_loggerMock.Object, httpClient);

        await service.GetMostDiscussedTopicsByPersonId(7, CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.test.com/person/7/topics/most-discussed", handler.LastRequest.RequestUri!.ToString());
    }

    private sealed class MockHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public MockHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseContent;

        public CapturingHandler(string responseContent)
        {
            _responseContent = responseContent;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            });
        }
    }
}
