using System.Net;
using System.Net.Http.Json;

using Moq;

using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests.Features.Topics;

public class TopicEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TopicEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTopicById_WithExistingTopic_ReturnsOk()
    {
        // Arrange
        Topic expected = new(1, "PlayStation 5");
        _factory.TopicServiceMock
            .Setup(s => s.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Topic? result = await response.Content.ReadFromJsonAsync<Topic>();
        Assert.NotNull(result);
        Assert.Equal("PlayStation 5", result.Name);
    }

    [Fact]
    public async Task GetTopicById_WithNonExistentTopic_ReturnsNotFound()
    {
        // Arrange
        _factory.TopicServiceMock
            .Setup(s => s.GetById(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Topic?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTopicById_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        _factory.TopicServiceMock
            .Setup(s => s.GetById(-1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("id"));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTopicTimeline_WithExistingTopic_ReturnsOk()
    {
        // Arrange
        TopicTimeline expected = new(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 6, 1),
            new PagedResponse<TopicTimelineEntry>(
                [new TopicTimelineEntry(1, "Episode A", new DateOnly(2024, 1, 1), "https://patreon.com/1", [new Person(1, "Person A")])],
                1, 1, 50));
        _factory.TopicServiceMock
            .Setup(s => s.GetTimeline(1, It.IsAny<PagedItemRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TopicTimeline? result = await response.Content.ReadFromJsonAsync<TopicTimeline>();
        Assert.NotNull(result);
        Assert.Single(result.Entries.Items);
        Assert.Equal(new DateOnly(2024, 1, 1), result.FirstDiscussed);
    }

    [Fact]
    public async Task GetTopicTimeline_WithNonExistentTopic_ReturnsNotFound()
    {
        // Arrange
        _factory.TopicServiceMock
            .Setup(s => s.GetTimeline(9999, It.IsAny<PagedItemRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicTimeline?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/9999/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMostDiscussedAlongside_WithExistingTopic_ReturnsOk()
    {
        // Arrange
        List<MostDiscussedTopic> expected =
        [
            new MostDiscussedTopic(2, "Related Topic A", 5),
            new MostDiscussedTopic(3, "Related Topic B", 3)
        ];
        _factory.TopicServiceMock
            .Setup(s => s.GetMostDiscussedAlongsideByTopicId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/most-discussed-alongside");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<MostDiscussedTopic>? result = await response.Content.ReadFromJsonAsync<List<MostDiscussedTopic>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Related Topic A", result[0].Name);
        Assert.Equal(5, result[0].EpisodeCount);
    }

    [Fact]
    public async Task GetMostDiscussedAlongside_WithNoCoOccurringTopics_ReturnsOkWithEmptyList()
    {
        // Arrange
        _factory.TopicServiceMock
            .Setup(s => s.GetMostDiscussedAlongsideByTopicId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/most-discussed-alongside");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<MostDiscussedTopic>? result = await response.Content.ReadFromJsonAsync<List<MostDiscussedTopic>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
