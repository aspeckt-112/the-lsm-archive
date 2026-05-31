using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Search;

public sealed class SearchEndpointsTests(IntegrationTestFixture fixture) : EndpointIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Search_WhenMatchesExist_ReturnsOkPagedResults()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Search Test Show");

        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Retro Alpha", cancellationToken);
        TopicEntity topic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Retro Beta", cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Retro Gamma",
            new DateTimeOffset(2026, 6, 7, 12, 0, 0, TimeSpan.Zero),
            8101,
            cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            "/search/?searchTerm=retro&entityType=All&pageNumber=2&pageSize=1",
            cancellationToken);

        PagedResponse<SearchResult>? responseBody =
            await response.Content.ReadFromJsonAsync<PagedResponse<SearchResult>>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        NotNull(responseBody);
        Equal(3, responseBody.TotalCount);
        Equal(2, responseBody.PageNumber);
        Equal(1, responseBody.PageSize);
        Equal(3, responseBody.TotalPages);
        Single(responseBody.Items);
        Equal([new SearchResult(topic.Id, "Retro Beta", EntityType.Topic)], responseBody.Items);
    }

    [Fact]
    public async Task Search_WhenNoMatchesExist_ReturnsNoContent()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            "/search/?searchTerm=doesnotexist&entityType=All",
            TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenPageSizeIsInvalid_ReturnsBadRequestProblemDetails()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            "/search/?searchTerm=retro&entityType=All&pageSize=0",
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.BadRequest, response.StatusCode);
        NotNull(problemDetails);
        Equal(400, problemDetails.Status);
        Equal("Bad Request", problemDetails.Title);
        Equal("ArgumentOutOfRangeException", problemDetails.Type);
        Contains("pageSize", problemDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_WhenEntityTypeIsUnsupported_ReturnsInternalServerErrorProblemDetails()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(
            "/search/?searchTerm=retro&entityType=999",
            TestContext.Current.CancellationToken);

        ProblemDetails? problemDetails =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        NotNull(problemDetails);
        Equal(500, problemDetails.Status);
        Equal("Internal Server Error", problemDetails.Title);
        Equal("An error occurred while processing your request.", problemDetails.Detail);
        Equal("UnsupportedEntityTypeException", problemDetails.Type);
    }
}
