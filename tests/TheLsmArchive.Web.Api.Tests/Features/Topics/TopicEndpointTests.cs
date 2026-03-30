using System.Net;
using System.Net.Http.Json;

using Moq;

using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
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
    public async Task GetTopicDetailsById_WithExistingTopic_ReturnsOk()
    {
        // Arrange
        TopicDetails expected = new(new DateOnly(2024, 2, 1), new DateOnly(2024, 8, 1));
        _factory.TopicServiceMock
            .Setup(s => s.GetDetailsById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/details");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TopicDetails? result = await response.Content.ReadFromJsonAsync<TopicDetails>();
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 2, 1), result.FirstDiscussed);
    }

    [Fact]
    public async Task GetTopicDetailsById_WithNonExistentTopic_ReturnsNotFound()
    {
        // Arrange
        _factory.TopicServiceMock
            .Setup(s => s.GetDetailsById(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TopicDetails?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/9999/details");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEpisodesByTopicId_ReturnsOk()
    {
        // Arrange
        List<Episode> expected = [new(1, "Episode A", new DateOnly(2024, 1, 1), "https://patreon.com/1", "Summary")];
        _factory.EpisodeServiceMock
            .Setup(s => s.GetByTopicId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/episodes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Episode>? result = await response.Content.ReadFromJsonAsync<List<Episode>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetPeopleByTopicId_ReturnsOk()
    {
        // Arrange
        List<Person> expected = [new(1, "Person A"), new(2, "Person B")];
        _factory.PersonServiceMock
            .Setup(s => s.GetByTopicId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/topic/1/people");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Person>? result = await response.Content.ReadFromJsonAsync<List<Person>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
