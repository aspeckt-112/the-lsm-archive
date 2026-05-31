using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Persons;

public sealed class PersonEndpointsTests(IntegrationTestFixture fixture) : EndpointIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetPersonById_WhenPersonExists_ReturnsOkPerson()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Colin Moriarty", cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}", cancellationToken);
        Person? responseBody = await response.Content.ReadFromJsonAsync<Person>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal(new Person(person.Id, "Colin Moriarty"), responseBody);
    }

    [Fact]
    public async Task GetPersonById_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/person/999999", TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPersonById_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}", TestContext.Current.CancellationToken);
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
    public async Task GetPersonDetailsById_WhenPersonExists_ReturnsOkPersonDetails()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Matty", cancellationToken);

        EpisodeEntity earliestEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Early Appearance",
            new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            7201,
            cancellationToken);

        EpisodeEntity latestEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Latest Appearance",
            new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
            7202,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, latestEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, earliestEpisode, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/details", cancellationToken);
        PersonDetails? responseBody = await response.Content.ReadFromJsonAsync<PersonDetails>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal(
            new PersonDetails(
                FirstAppeared: new DateOnly(2026, 1, 15),
                LastAppeared: new DateOnly(2026, 5, 15)),
            responseBody);
    }

    [Fact]
    public async Task GetPersonDetailsById_WhenPersonDoesNotExist_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/person/999999/details", TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPersonDetailsById_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}/details", TestContext.Current.CancellationToken);
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
    public async Task GetTopicsByPersonId_WhenTopicsExist_ReturnsPagedFilteredTopics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Dustin Furman", cancellationToken);
        TopicEntity xbox = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Xbox", cancellationToken);
        TopicEntity xbox360 = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Xbox 360", cancellationToken);
        TopicEntity playstation = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "PlayStation", cancellationToken);

        await PersonTestDataHelper.LinkTopicToPersonAsync(dbContext, person, xbox, cancellationToken);
        await PersonTestDataHelper.LinkTopicToPersonAsync(dbContext, person, xbox360, cancellationToken);
        await PersonTestDataHelper.LinkTopicToPersonAsync(dbContext, person, playstation, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            $"/person/{person.Id}/topics?pageNumber=2&pageSize=1&searchTerm=xbox&sortDescending=true",
            cancellationToken);

        PagedResponse<Topic>? responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<Topic>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(2, responseBody.TotalCount);
        Equal(2, responseBody.PageNumber);
        Equal(1, responseBody.PageSize);
        Equal(2, responseBody.TotalPages);
        Equal([new Topic(xbox.Id, "Xbox")], responseBody.Items);
    }

    [Fact]
    public async Task GetTopicsByPersonId_WhenPersonHasNoTopics_ReturnsEmptyPagedResponse()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Ben Smith", cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/topics", cancellationToken);
        PagedResponse<Topic>? responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<Topic>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Empty(responseBody.Items);
        Equal(0, responseBody.TotalCount);
        Equal(1, responseBody.PageNumber);
        Equal(50, responseBody.PageSize);
        Equal(0, responseBody.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTopicsByPersonId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}/topics", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WhenTopicsExist_ReturnsRankedTopics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Chris Ray Gun", cancellationToken);
        TopicEntity xbox = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Xbox", cancellationToken);
        TopicEntity gaming = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Gaming", cancellationToken);
        TopicEntity politics = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Politics", cancellationToken);

        EpisodeEntity firstEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode One",
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
            7301,
            cancellationToken);

        EpisodeEntity secondEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode Two",
            new DateTimeOffset(2026, 2, 10, 12, 0, 0, TimeSpan.Zero),
            7302,
            cancellationToken);

        EpisodeEntity thirdEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode Three",
            new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero),
            7303,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, firstEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, secondEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, thirdEpisode, cancellationToken);

        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, xbox, firstEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, gaming, firstEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, xbox, secondEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, politics, secondEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, xbox, thirdEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, gaming, thirdEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, politics, thirdEpisode, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/topics/most-discussed", cancellationToken);
        List<MostDiscussedTopic>? responseBody = await response.Content.ReadFromJsonAsync<List<MostDiscussedTopic>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal(
            [
                new MostDiscussedTopic(xbox.Id, "Xbox", 3),
                new MostDiscussedTopic(gaming.Id, "Gaming", 2),
                new MostDiscussedTopic(politics.Id, "Politics", 2)
            ],
            responseBody);
    }

    [Fact]
    public async Task GetMostDiscussedTopicsByPersonId_WhenPersonHasNoEpisodes_ReturnsEmptyList()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Stelly", cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/topics/most-discussed", cancellationToken);
        List<MostDiscussedTopic>? responseBody = await response.Content.ReadFromJsonAsync<List<MostDiscussedTopic>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Empty(responseBody);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMostDiscussedTopicsByPersonId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}/topics/most-discussed", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }

    [Fact]
    public async Task GetEpisodesByPersonId_WhenEpisodesExist_ReturnsPagedFilteredTimeline()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Dustin Furman", cancellationToken);
        TopicEntity nintendoTopic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Nintendo", cancellationToken);
        TopicEntity communityTopic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Community", cancellationToken);

        EpisodeEntity titleMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Nintendo Direct Reactions",
            new DateTimeOffset(2026, 2, 10, 14, 0, 0, TimeSpan.Zero),
            7401,
            cancellationToken);

        EpisodeEntity topicMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Mailbag Special",
            new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero),
            7402,
            cancellationToken);

        EpisodeEntity nonMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "PlayStation Showcase",
            new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero),
            7403,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, titleMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, topicMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, nonMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, communityTopic, titleMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, nintendoTopic, topicMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, communityTopic, nonMatch, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            $"/person/{person.Id}/episodes?pageNumber=1&pageSize=2&searchTerm=nintendo&sortDescending=true",
            cancellationToken);

        PagedResponse<PersonTimelineEntry>? responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<PersonTimelineEntry>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(2, responseBody.TotalCount);
        Equal(1, responseBody.PageNumber);
        Equal(2, responseBody.PageSize);
        Equal(1, responseBody.TotalPages);
        Equal(2, responseBody.Items.Count);

        PersonTimelineEntry mostRecentMatch = responseBody.Items[0];
        Equal(topicMatch.Id, mostRecentMatch.EpisodeId);
        Equal("Mailbag Special", mostRecentMatch.Title);
        Equal(new DateOnly(2026, 4, 10), mostRecentMatch.ReleaseDate);
        Equal("https://www.patreon.com/posts/7402", mostRecentMatch.PatreonPostLink);
        Equal([new Topic(nintendoTopic.Id, "Nintendo")], mostRecentMatch.Topics);

        PersonTimelineEntry olderMatch = responseBody.Items[1];
        Equal(titleMatch.Id, olderMatch.EpisodeId);
        Equal("Nintendo Direct Reactions", olderMatch.Title);
        Equal(new DateOnly(2026, 2, 10), olderMatch.ReleaseDate);
        Equal("https://www.patreon.com/posts/7401", olderMatch.PatreonPostLink);
        Equal([new Topic(communityTopic.Id, "Community")], olderMatch.Topics);
    }

    [Fact]
    public async Task GetEpisodesByPersonId_WhenPersonHasNoEpisodes_ReturnsEmptyPagedResponse()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Ben Smith", cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/episodes", cancellationToken);
        PagedResponse<PersonTimelineEntry>? responseBody = await response.Content.ReadFromJsonAsync<PagedResponse<PersonTimelineEntry>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Empty(responseBody.Items);
        Equal(0, responseBody.TotalCount);
        Equal(1, responseBody.PageNumber);
        Equal(50, responseBody.PageSize);
        Equal(0, responseBody.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetEpisodesByPersonId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}/episodes", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }

    [Fact]
    public async Task GetLatestEpisodeByPersonId_WhenEpisodesExist_ReturnsMostRecentEpisode()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Matty", cancellationToken);

        EpisodeEntity olderEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Older Appearance",
            new DateTimeOffset(2026, 2, 20, 12, 0, 0, TimeSpan.Zero),
            7501,
            cancellationToken,
            summaryHtml: "<p>Older summary.</p>");

        EpisodeEntity latestEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Latest Appearance",
            new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero),
            7502,
            cancellationToken,
            summaryHtml: "<p>Latest summary.</p>");

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, olderEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, latestEpisode, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/episodes/latest", cancellationToken);
        Episode? responseBody = await response.Content.ReadFromJsonAsync<Episode>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal(
            new Episode(
                Id: latestEpisode.Id,
                Title: "Latest Appearance",
                ReleaseDate: new DateOnly(2026, 5, 20),
                PatreonPostLink: "https://www.patreon.com/posts/7502",
                SummaryHtml: "<p>Latest summary.</p>"),
            responseBody);
    }

    [Fact]
    public async Task GetLatestEpisodeByPersonId_WhenPersonHasNoEpisodes_ReturnsNotFound()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Stelly", cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{person.Id}/episodes/latest", cancellationToken);

        // Assert
        Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLatestEpisodeByPersonId_WhenIdIsInvalid_ReturnsBadRequestProblemDetails(int id)
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/person/{id}/episodes/latest", TestContext.Current.CancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
    }
}


