using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.System;

public sealed class SystemEndpointsTests(IntegrationTestFixture fixture) : EndpointIntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetLastDataSyncDateTime_WhenProcessedPostsExist_ReturnsOkWithMostRecentPublishedDate()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");
        DateTimeOffset expected = new(2026, 5, 21, 14, 30, 0, TimeSpan.Zero);

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(dbContext, show,
            new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.Zero), 2001, cancellationToken);

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(dbContext, show, expected, 2002, cancellationToken);

        await SystemTestDataHelper.CreateUnprocessedPatreonPostAsync(dbContext, show,
            new DateTimeOffset(2026, 5, 21, 15, 30, 0, TimeSpan.Zero), 2003, cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/system/last-data-sync", cancellationToken);
        DateTimeOffset? responseBody = await response.Content.ReadFromJsonAsync<DateTimeOffset>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal(expected, responseBody);
    }

    [Fact]
    public async Task GetLastDataSyncDateTime_WhenNoProcessedPostsExist_ReturnsInternalServerErrorProblemDetails()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");

        await SystemTestDataHelper.CreateUnprocessedPatreonPostAsync(
            dbContext,
            show,
            new DateTimeOffset(2026, 5, 21, 15, 30, 0, TimeSpan.Zero),
            2001,
            cancellationToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/system/last-data-sync", cancellationToken);
        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // Assert
        Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        NotNull(problemDetails);
        Equal(500, problemDetails.Status);
        Equal("Internal Server Error", problemDetails.Title);
        Equal("An error occurred while processing your request.", problemDetails.Detail);
        Equal("InvalidOperationException", problemDetails.Type);
    }

    [Fact]
    public async Task GetLastDataSyncDateTime_WhenOriginIsAllowed_ReturnsCorsHeader()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await SeedProcessedPatreonPostAsync(cancellationToken);

        using HttpRequestMessage request = new(HttpMethod.Get, "/system/last-data-sync");
        request.Headers.Add("Origin", "https://lsmarchive.com");

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? headerValues);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        Equal("https://lsmarchive.com", headerValues?.Single());
    }

    [Fact]
    public async Task GetLastDataSyncDateTime_WhenOriginIsNotAllowed_DoesNotReturnCorsHeader()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await SeedProcessedPatreonPostAsync(cancellationToken);

        using HttpRequestMessage request = new(HttpMethod.Get, "/system/last-data-sync");
        request.Headers.Add("Origin", "https://example.com");

        // Act
        HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);

        // Assert
        Equal(HttpStatusCode.OK, response.StatusCode);
        False(response.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
    }

    private async Task SeedProcessedPatreonPostAsync(CancellationToken cancellationToken)
    {
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(
            dbContext,
            show,
            new DateTimeOffset(2026, 5, 21, 14, 30, 0, TimeSpan.Zero),
            2001,
            cancellationToken);
    }
}
