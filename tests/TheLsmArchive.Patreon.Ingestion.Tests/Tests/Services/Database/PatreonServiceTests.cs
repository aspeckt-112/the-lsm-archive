using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
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

    [Fact]
    public async Task IngestFeed_WhenFeedIsNew_CreatesPatreonPosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity showEntity = CreateShowEntity("Test Show");

        await dbContext.Shows.AddAsync(showEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PatreonPost> posts =
        [
            new PatreonPost(
                Id: 1,
                Title: "Test Post 1",
                Published: DateTimeOffset.UtcNow.AddDays(-2),
                Summary: "Summary of Test Post 1",
                Link: "https://www.patreon.com/posts/test-post-1",
                AudioUrl: "https://www.patreon.com/posts/test-post-1/audio"),
            new PatreonPost(
                Id: 2,
                Title: "Test Post 2",
                Published: DateTimeOffset.UtcNow.AddDays(-1),
                Summary: "Summary of Test Post 2",
                Link: "https://www.patreon.com/posts/test-post-2",
                AudioUrl: "https://www.patreon.com/posts/test-post-2/audio")
        ];

        PatreonFeed feed = new("Test Feed", posts);

        PatreonService patreonService = Get<PatreonService>();

        // Act
        await patreonService.IngestFeed(showEntity.Id, feed, cancellationToken);

        // Assert
        List<PatreonPostEntity> createdPostEntities = await dbContext.PatreonPosts
            .Where(p => p.ShowId == showEntity.Id)
            .OrderBy(p => p.PatreonId)
            .ToListAsync(cancellationToken);

        NotNull(createdPostEntities);
        Equal(2, createdPostEntities.Count);

        PatreonPostEntity postEntity1 = createdPostEntities[0];
        Equal(showEntity.Id, postEntity1.ShowId);
        Equal(1, postEntity1.PatreonId);
        Equal("Test Post 1", postEntity1.Title);
        Equal(posts[0].Published, postEntity1.Published);
        Equal("Summary of Test Post 1", postEntity1.Summary);
        Equal("https://www.patreon.com/posts/test-post-1", postEntity1.Link);
        Equal("https://www.patreon.com/posts/test-post-1/audio", postEntity1.AudioUrl);

        PatreonPostEntity postEntity2 = createdPostEntities[1];
        Equal(showEntity.Id, postEntity2.ShowId);
        Equal(2, postEntity2.PatreonId);
        Equal("Test Post 2", postEntity2.Title);
        Equal(posts[1].Published, postEntity2.Published);
        Equal("Summary of Test Post 2", postEntity2.Summary);
        Equal("https://www.patreon.com/posts/test-post-2", postEntity2.Link);
        Equal("https://www.patreon.com/posts/test-post-2/audio", postEntity2.AudioUrl);
    }

    [Fact]
    public async Task IngestFeed_WhenFeedHasExistingPosts_DoesNotCreateDuplicatePosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity showEntity = CreateShowEntity("Test Show");

        await dbContext.Shows.AddAsync(showEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonPostEntity existingPostEntity = new()
        {
            ShowId = showEntity.Id,
            PatreonId = 1,
            Title = "Existing Test Post",
            Published = DateTimeOffset.UtcNow.AddDays(-3),
            Summary = "Summary of Existing Test Post",
            Link = "https://www.patreon.com/posts/existing-test-post",
            AudioUrl = "https://www.patreon.com/posts/existing-test-post/audio"
        };

        await dbContext.PatreonPosts.AddAsync(existingPostEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PatreonPost> posts =
        [
            new PatreonPost(
                Id: 1,
                Title: "Existing Test Post",
                Published: DateTimeOffset.UtcNow.AddDays(-3),
                Summary: "Summary of Existing Test Post",
                Link: "https://www.patreon.com/posts/existing-test-post",
                AudioUrl: "https://www.patreon.com/posts/existing-test-post/audio"),
            new PatreonPost(
                Id: 2,
                Title: "New Test Post",
                Published: DateTimeOffset.UtcNow.AddDays(-1),
                Summary: "Summary of New Test Post",
                Link: "https://www.patreon.com/posts/new-test-post",
                AudioUrl: "https://www.patreon.com/posts/new-test-post/audio")
        ];

        PatreonFeed feed = new("Test Feed", posts);

        PatreonService patreonService = Get<PatreonService>();

        // Act
        await patreonService.IngestFeed(showEntity.Id, feed, cancellationToken);

        // Assert
        List<PatreonPostEntity> postEntities = await dbContext.PatreonPosts
            .Where(p => p.ShowId == showEntity.Id)
            .OrderBy(p => p.PatreonId)
            .ToListAsync(cancellationToken);

        NotNull(postEntities);
        Equal(2, postEntities.Count);

        PatreonPostEntity postEntity1 = postEntities[0];
        Equal(existingPostEntity.Id, postEntity1.Id);
        Equal(showEntity.Id, postEntity1.ShowId);
        Equal(1, postEntity1.PatreonId);
        Equal("Existing Test Post", postEntity1.Title);
        Equal(existingPostEntity.Published, postEntity1.Published);
        Equal("Summary of Existing Test Post", postEntity1.Summary);
        Equal("https://www.patreon.com/posts/existing-test-post", postEntity1.Link);
        Equal("https://www.patreon.com/posts/existing-test-post/audio", postEntity1.AudioUrl);

        PatreonPostEntity postEntity2 = postEntities[1];
        Equal(showEntity.Id, postEntity2.ShowId);
        Equal(2, postEntity2.PatreonId);
        Equal("New Test Post", postEntity2.Title);
        Equal(posts[1].Published, postEntity2.Published);
        Equal("Summary of New Test Post", postEntity2.Summary);
        Equal("https://www.patreon.com/posts/new-test-post", postEntity2.Link);
        Equal("https://www.patreon.com/posts/new-test-post/audio", postEntity2.AudioUrl);
    }

    private ShowEntity CreateShowEntity(string name) => new() { Name = name };
}
