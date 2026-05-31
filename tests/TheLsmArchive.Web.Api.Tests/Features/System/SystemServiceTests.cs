using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;
using TheLsmArchive.Web.Api.Features.System;

namespace TheLsmArchive.Web.Api.Tests.Features.System;

public sealed class SystemServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WhenProcessedPostsExist_ReturnsMostRecentPublishedDate()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");
        DateTimeOffset oldestProcessedPublished = new(2026, 5, 18, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset latestProcessedPublished = new(2026, 5, 21, 9, 30, 0, TimeSpan.Zero);

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(dbContext, show, oldestProcessedPublished, 1001,
            cancellationToken);

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(dbContext, show, latestProcessedPublished, 1002,
            cancellationToken);

        ISystemService systemService = Get<ISystemService>();

        // Act
        DateTimeOffset result = await systemService.GetLastDataSyncDateTimeAsync(cancellationToken);

        // Assert
        Equal(latestProcessedPublished, result);
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WhenLatestPostIsUnprocessed_IgnoresIt()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");
        DateTimeOffset processedPublished = new(2026, 5, 20, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset unprocessedPublished = new(2026, 5, 21, 8, 0, 0, TimeSpan.Zero);

        await SystemTestDataHelper.CreateProcessedPatreonPostAsync(dbContext, show, processedPublished, 1001,
            cancellationToken);

        await SystemTestDataHelper.CreateUnprocessedPatreonPostAsync(dbContext, show, unprocessedPublished, 1002,
            cancellationToken);

        ISystemService systemService = Get<ISystemService>();

        // Act
        DateTimeOffset result = await systemService.GetLastDataSyncDateTimeAsync(cancellationToken);

        // Assert
        Equal(processedPublished, result);
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WhenNoProcessedPostsExist_ThrowsInvalidOperationException()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "System Test Show");

        await SystemTestDataHelper.CreateUnprocessedPatreonPostAsync(
            dbContext,
            show,
            new DateTimeOffset(2026, 5, 21, 8, 0, 0, TimeSpan.Zero),
            1001,
            cancellationToken);

        ISystemService systemService = Get<ISystemService>();

        // Act & Assert
        await ThrowsAsync<InvalidOperationException>(() =>
            systemService.GetLastDataSyncDateTimeAsync(cancellationToken));
    }
}
