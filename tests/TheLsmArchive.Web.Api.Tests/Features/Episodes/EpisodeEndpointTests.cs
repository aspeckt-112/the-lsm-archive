using System.Net;
using System.Net.Http.Json;

using Moq;

using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

public class EpisodeEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EpisodeEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetEpisodeById_WithExistingEpisode_ReturnsOk()
    {
        // Arrange
        Episode expected = new(1, "Test Episode", new DateOnly(2024, 5, 10), "https://patreon.com/1", "<p>Summary</p>");
        _factory.EpisodeServiceMock
            .Setup(s => s.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Episode? result = await response.Content.ReadFromJsonAsync<Episode>();
        Assert.NotNull(result);
        Assert.Equal("Test Episode", result.Title);
    }

    [Fact]
    public async Task GetEpisodeById_WithNonExistentEpisode_ReturnsNotFound()
    {
        // Arrange
        _factory.EpisodeServiceMock
            .Setup(s => s.GetById(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEpisodeById_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange — ArgumentOutOfRangeException is mapped to 400 by GlobalExceptionHandler
        _factory.EpisodeServiceMock
            .Setup(s => s.GetById(-1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("id"));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPeopleByEpisodeId_ReturnsOk()
    {
        // Arrange
        List<Person> expected = [new(1, "Person A"), new(2, "Person B")];
        _factory.PersonServiceMock
            .Setup(s => s.GetByEpisodeId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/1/people");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Person>? result = await response.Content.ReadFromJsonAsync<List<Person>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTopicsByEpisodeId_ReturnsOk()
    {
        // Arrange
        List<Topic> expected = [new(1, "Topic A")];
        _factory.TopicServiceMock
            .Setup(s => s.GetByEpisodeId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/1/topics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<Topic>? result = await response.Content.ReadFromJsonAsync<List<Topic>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecentEpisodes_WithEpisodes_ReturnsOk()
    {
        // Arrange
        List<Episode> expected = [new(1, "Recent Ep", new DateOnly(2024, 5, 10), "https://patreon.com/1", "<p>Summary</p>")];
        _factory.EpisodeServiceMock
            .Setup(s => s.GetRecent(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/recent");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentEpisodes_WithNoEpisodes_ReturnsNoContent()
    {
        // Arrange
        _factory.EpisodeServiceMock
            .Setup(s => s.GetRecent(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/recent");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetRandomEpisodeId_ReturnsOk()
    {
        // Arrange
        _factory.EpisodeServiceMock
            .Setup(s => s.GetRandomEpisodeId(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/episode/random");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        int? result = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(42, result);
    }
}
