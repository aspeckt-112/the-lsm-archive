using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Fixtures;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services.Database;

public sealed class EpisodeServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreate_WhenNoExistingEpisode_CreatesAndReturnsNewEpisode()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, PatreonPostEntity postEntity) = await GetSeededDbContext(cancellationToken);

        EpisodeService episodeService = Get<EpisodeService>();

        // Act
        int episodeId = await episodeService.GetOrCreateAsync(postEntity, cancellationToken);

        // Assert
        True(episodeId > 0);

        EpisodeEntity? persisted = await dbContext.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PatreonPostId == postEntity.Id, cancellationToken);

        NotNull(persisted);
        Equal(episodeId, persisted.Id);
        Equal(postEntity.ShowId, persisted.ShowId);
        Equal(postEntity.Title, persisted.Title);
        Equal(postEntity.Published.UtcDateTime, persisted.ReleaseDateUtc);
        Equal(postEntity.Id, persisted.PatreonPostId);
    }

    [Fact]
    public async Task GetOrCreate_WhenEpisodeAlreadyExists_ReturnsExistingEpisodeWithoutCreatingDuplicate()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, PatreonPostEntity postEntity) = await GetSeededDbContext(cancellationToken);

        EpisodeEntity existingEpisode = new()
        {
            ShowId = postEntity.ShowId,
            Title = postEntity.Title,
            ReleaseDateUtc = postEntity.Published.UtcDateTime,
            PatreonPostId = postEntity.Id
        };

        dbContext.Episodes.Add(existingEpisode);
        await dbContext.SaveChangesAsync(cancellationToken);

        EpisodeService episodeService = Get<EpisodeService>();

        // Act
        int result = await episodeService.GetOrCreateAsync(postEntity, cancellationToken);

        // Assert
        Equal(existingEpisode.Id, result);

        int episodeCount = await dbContext.Episodes
            .CountAsync(e => e.PatreonPostId == postEntity.Id, cancellationToken);

        Equal(1, episodeCount);
    }

    private async Task<(LsmArchiveDbContext dbContext, PatreonPostEntity postEntity)> GetSeededDbContext(
        CancellationToken cancellationToken)
    {
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity show = new() { Name = "Test Show" };
        dbContext.Shows.Add(show);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonPostEntity post = new()
        {
            ShowId = show.Id,
            PatreonId = 1,
            Title = "Test Episode Post",
            Published = DateTimeOffset.UnixEpoch.AddDays(1),
            Summary = "A test post summary",
            Link = "https://www.patreon.com/posts/test-1",
            AudioUrl = "https://www.patreon.com/posts/test-1/audio"
        };

        dbContext.PatreonPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (dbContext, post);
    }
}
