using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public sealed class TopicServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreate_WhenTopicDoesNotExist_CreatesAndReturnsNewTopic()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        TopicService topicService = Get<TopicService>();

        // Act
        int topicId = await topicService.GetOrCreateAsync("Dark Souls", cancellationToken);

        // Assert
        True(topicId > 0);

        TopicEntity? persisted = await dbContext.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);

        NotNull(persisted);
        Equal("Dark Souls", persisted.Name);
    }

    [Fact]
    public async Task GetOrCreate_WhenTopicExistsByNormalizedName_ReturnsExistingTopic()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        TopicEntity existing = new() { Name = "Elden Ring", NormalizedName = "eldenring" };
        dbContext.Topics.Add(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        TopicService topicService = Get<TopicService>();

        // Act — same name produces same normalized key
        int result = await topicService.GetOrCreateAsync("Elden Ring", cancellationToken);

        // Assert
        Equal(existing.Id, result);

        int count = await dbContext.Topics.CountAsync(cancellationToken);
        Equal(1, count);
    }

    [Fact]
    public async Task GetOrCreate_WhenTopicExistsByFuzzyMatch_ReturnsExistingTopic()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        TopicEntity existing = new() { Name = "Bloodborne", NormalizedName = "bloodborne" };
        dbContext.Topics.Add(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        TopicService topicService = Get<TopicService>();

        // Act — slight variation that passes trigram similarity > 0.8 but has a different normalized key
        int result = await topicService.GetOrCreateAsync("Bloodborne ", cancellationToken);

        // Assert
        Equal(existing.Id, result);
    }

    [Fact]
    public async Task GetOrCreate_WithLeadingAndTrailingWhitespace_TrimsNameBeforeProcessing()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        TopicService topicService = Get<TopicService>();

        // Act
        int topicId = await topicService.GetOrCreateAsync("  Sekiro  ", cancellationToken);

        // Assert
        TopicEntity? persisted = await dbContext.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);

        NotNull(persisted);
        Equal("Sekiro", persisted.Name);
    }
}
