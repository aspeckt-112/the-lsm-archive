using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Web.Api.Features.System;

namespace TheLsmArchive.Web.Api.Tests.Features.System;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class SystemServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly SystemService _systemService;

    public SystemServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<SystemService>> loggerMock = new();

        _systemService = new SystemService(
            loggerMock.Object,
            DbContext
        );
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WithLinkedPatreonPosts_ReturnsMostRecentPublished()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post1 = new()
        {
            PatreonId = 1,
            Title = "Post 1",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero),
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        PatreonPostEntity post2 = new()
        {
            PatreonId = 2,
            Title = "Post 2",
            Link = "https://patreon.com/2",
            Summary = "Summary 2",
            Published = new DateTimeOffset(2024, 6, 20, 12, 0, 0, TimeSpan.Zero),
            AudioUrl = "https://audio.com/2",
            ShowId = show.Id
        };

        EpisodeEntity ep1 = new()
        {
            Title = "Episode 1",
            ReleaseDateUtc = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post1,
            ShowId = show.Id
        };
        EpisodeEntity ep2 = new()
        {
            Title = "Episode 2",
            ReleaseDateUtc = new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        // Link posts to episodes via a separate tracked DbContext instance.
        PatreonPostEntity trackedPost1 = (await WriteDbContext.PatreonPosts.FindAsync([post1.Id], TestContext.Current.CancellationToken))!;
        PatreonPostEntity trackedPost2 = (await WriteDbContext.PatreonPosts.FindAsync([post2.Id], TestContext.Current.CancellationToken))!;
        trackedPost1.EpisodeId = ep1.Id;
        trackedPost2.EpisodeId = ep2.Id;
        await WriteDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        DateTimeOffset lastSync = await _systemService.GetLastDataSyncDateTimeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new DateTimeOffset(2024, 6, 20, 12, 0, 0, TimeSpan.Zero), lastSync);
    }

    [Fact]
    public async Task GetLastDataSyncDateTimeAsync_WithNoLinkedPosts_ThrowsInvalidOperationException()
    {
        // Arrange — insert a post without linking to an episode (EpisodeId = null)
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity unlinkedPost = new()
        {
            PatreonId = 1,
            Title = "Unlinked Post",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        await InsertSingleInstanceOfEntityAsync(unlinkedPost);

        // Act & Assert — no posts with EpisodeId != null → FirstAsync throws
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _systemService.GetLastDataSyncDateTimeAsync(TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }
}
