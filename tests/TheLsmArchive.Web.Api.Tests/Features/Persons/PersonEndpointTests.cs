using System.Net;
using System.Net.Http.Json;

using Moq;

using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests.Features.Persons;

public class PersonEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PersonEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAllMocks();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPersonById_WithExistingPerson_ReturnsOk()
    {
        // Arrange
        Person expected = new(1, "Colin Moriarty");
        _factory.PersonServiceMock
            .Setup(s => s.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Person? result = await response.Content.ReadFromJsonAsync<Person>();
        Assert.NotNull(result);
        Assert.Equal("Colin Moriarty", result.Name);
    }

    [Fact]
    public async Task GetPersonById_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        _factory.PersonServiceMock
            .Setup(s => s.GetById(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPersonById_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        _factory.PersonServiceMock
            .Setup(s => s.GetById(-1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("id"));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/-1");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPersonDetailsById_WithExistingPerson_ReturnsOk()
    {
        // Arrange
        PersonDetails expected = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 1));
        _factory.PersonServiceMock
            .Setup(s => s.GetDetailsById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/1/details");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PersonDetails? result = await response.Content.ReadFromJsonAsync<PersonDetails>();
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 1, 1), result.FirstAppeared);
    }

    [Fact]
    public async Task GetPersonDetailsById_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        _factory.PersonServiceMock
            .Setup(s => s.GetDetailsById(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDetails?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/9999/details");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTopicsByPersonId_ReturnsOk()
    {
        // Arrange
        PagedResponse<Topic> expected = new([new(1, "Topic A")], 1, 1, 50);
        _factory.TopicServiceMock
            .Setup(s => s.GetByPersonId(1, It.IsAny<PagedItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/1/topics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEpisodesByPersonId_ReturnsOk()
    {
        // Arrange
        PagedResponse<Episode> expected = new(
            [new(1, "Episode A", new DateOnly(2024, 1, 1), "https://patreon.com/1", "Summary")], 1, 1, 50);
        _factory.EpisodeServiceMock
            .Setup(s => s.GetByPersonId(1, It.IsAny<PagedItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/1/episodes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestEpisodeByPersonId_WithEpisode_ReturnsOk()
    {
        // Arrange
        Episode expected = new(1, "Latest Ep", new DateOnly(2024, 6, 1), "https://patreon.com/1", "Summary");
        _factory.EpisodeServiceMock
            .Setup(s => s.GetMostRecentByPersonId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/1/episodes/latest");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestEpisodeByPersonId_WithNoPerson_ReturnsNotFound()
    {
        // Arrange
        _factory.EpisodeServiceMock
            .Setup(s => s.GetMostRecentByPersonId(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Episode?)null);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/person/9999/episodes/latest");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
