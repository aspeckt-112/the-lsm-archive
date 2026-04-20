using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Infrastructure;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Tests.Services.Database;

public sealed class PatreonServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public void IngestFeed_WhenShowDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        PatreonService patreonService = Get<PatreonService>();

        const int nonExistentShowId = -1;

        PatreonFeed feed = new("Test Feed", []);

        // Act & Assert
        Throws<InvalidOperationException>(() =>
            patreonService.IngestFeed(nonExistentShowId, feed, cancellationToken).GetAwaiter().GetResult());
    }

    // [Fact]
    // public void IngestFeed_WhenFeedIsNew_CreatesFeedAndEpisodes()
    // {
    //     // Arrange
    //     CancellationToken cancellationToken = TestContext.Current.CancellationToken;

    //     LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

    //     // Act
    //     PatreonService patreonService = Get<PatreonService>();
    // }
}
