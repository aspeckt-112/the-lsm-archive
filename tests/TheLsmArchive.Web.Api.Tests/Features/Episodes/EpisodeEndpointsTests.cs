using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

public sealed class EpisodeEndpointsTests(IntegrationTestFixture fixture) : EndpointIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetEpisodeById_WhenEpisodeExists_ReturnsOkEpisode()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        DateTimeOffset releaseDateUtc = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Defining Duke 250",
            releaseDateUtc,
            6001,
            cancellationToken,
            summaryHtml: "<p>Xbox deep dive.</p>");

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{episode.Id}", cancellationToken);
        Episode? responseBody = await response.Content.ReadFromJsonAsync<Episode>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(episode.Id, responseBody.Id);
        Equal("Defining Duke 250", responseBody.Title);
        Equal(DateOnly.FromDateTime(releaseDateUtc.DateTime), responseBody.ReleaseDate);
        Equal("https://www.patreon.com/posts/6001", responseBody.PatreonPostLink);
        Equal("<p>Xbox deep dive.</p>", responseBody.SummaryHtml);
    }

    [Fact]
    public async Task GetEpisodeById_WhenEpisodeDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/episode/999999", TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetEpisodeById_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{id}", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
        Contains("id", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPeopleByEpisodeId_WhenPeopleExist_ReturnsAlphabeticallySortedPeople()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Sacred Symbols+",
            new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero),
            6101,
            cancellationToken);

        PersonEntity secondAlphabetically = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Colin Moriarty", cancellationToken);
        PersonEntity firstAlphabetically = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Ben Smith", cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, secondAlphabetically, episode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, firstAlphabetically, episode, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{episode.Id}/people", cancellationToken);
        List<Person>? responseBody = await response.Content.ReadFromJsonAsync<List<Person>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(
            [new Person(firstAlphabetically.Id, "Ben Smith"), new Person(secondAlphabetically.Id, "Colin Moriarty")],
            responseBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPeopleByEpisodeId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{id}/people", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }

    [Fact]
    public async Task GetTopicsByEpisodeId_WhenTopicsExist_ReturnsAlphabeticallySortedTopics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Constellation 100",
            new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero),
            6201,
            cancellationToken);

        TopicEntity secondAlphabetically = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Politics", cancellationToken);
        TopicEntity firstAlphabetically = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Gaming", cancellationToken);

        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, secondAlphabetically, episode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, firstAlphabetically, episode, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{episode.Id}/topics", cancellationToken);
        List<Topic>? responseBody = await response.Content.ReadFromJsonAsync<List<Topic>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(
            [new Topic(firstAlphabetically.Id, "Gaming"), new Topic(secondAlphabetically.Id, "Politics")],
            responseBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTopicsByEpisodeId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/episode/{id}/topics", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }

    [Fact]
    public async Task GetRecentEpisodes_WhenRecentEpisodesExist_ReturnsOkWithDescendingResults()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        EpisodeEntity olderRecentEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Older Recent Episode",
            now.AddDays(-6),
            6301,
            cancellationToken);

        EpisodeEntity newerRecentEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Newest Recent Episode",
            now.AddDays(-2),
            6302,
            cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Older Archive Episode",
            now.AddDays(-15),
            6303,
            cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/episode/recent", cancellationToken);
        List<Episode>? responseBody = await response.Content.ReadFromJsonAsync<List<Episode>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal([newerRecentEpisode.Id, olderRecentEpisode.Id], responseBody.Select(episode => episode.Id).ToList());
    }

    [Fact]
    public async Task GetRecentEpisodes_WhenNoRecentEpisodesExist_ReturnsNoContent()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Archive Episode",
            DateTimeOffset.UtcNow.AddDays(-14),
            6401,
            cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/episode/recent", cancellationToken);

        // Assert
        Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WhenEpisodesExist_ReturnsOkWithExistingEpisodeId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");

        EpisodeEntity firstEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode One",
            new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            6501,
            cancellationToken);

        EpisodeEntity secondEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode Two",
            new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero),
            6502,
            cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/episode/random", cancellationToken);
        int responseBody = await response.Content.ReadFromJsonAsync<int>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        int[] episodeIds = [firstEpisode.Id, secondEpisode.Id];
        Contains(responseBody, episodeIds);
    }
}





