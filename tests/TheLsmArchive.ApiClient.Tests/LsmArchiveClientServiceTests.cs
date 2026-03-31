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
