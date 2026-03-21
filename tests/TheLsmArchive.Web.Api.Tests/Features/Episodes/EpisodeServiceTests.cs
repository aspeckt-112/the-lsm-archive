using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class EpisodeServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly EpisodeService _episodeService;

    public EpisodeServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<EpisodeService>> loggerMock = new();

        _episodeService = new EpisodeService(
            loggerMock.Object,
            ReadOnlyDbContext
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetById_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange & Act & Assert
#pragma warning disable IDE0022 // Use expression body for method
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _episodeService.GetById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022 // Use expression body for method
    }

    [Fact]
    public async Task GetById_WithValidIdButNonExistentEpisode_ReturnsNull()
    {
        // Arrange & Act
        Episode? episode = await _episodeService.GetById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(episode);
    }

    [Fact]
    public async Task GetRecent_WithRecentEpisodes_ReturnsCorrectEpisodes()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post1 = new() { PatreonId = 1, Title = "Post 1", Link = "https://patreon.com/1", Summary = "Summary 1", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/1", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 2, Title = "Post 2", Link = "https://patreon.com/2", Summary = "Summary 2", Published = DateTimeOffset.UtcNow.AddDays(-10), AudioUrl = "https://audio.com/2", ShowId = show.Id };

        EpisodeEntity recentEpisode = new()
        {
            Title = "Recent Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity oldEpisode = new()
        {
            Title = "Old Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-10),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(recentEpisode);
        await InsertSingleInstanceOfEntityAsync(oldEpisode);

        // Act
        List<Episode> recentEpisodes = await _episodeService.GetRecent(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(recentEpisodes);
        Assert.Equal("Recent Episode", recentEpisodes[0].Title);
    }
}
