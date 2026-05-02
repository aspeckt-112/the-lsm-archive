using System.Collections.Immutable;

using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Fixtures;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services.Database;

public sealed class PatreonServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task IngestFeed_WhenShowDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        PatreonService patreonService = Get<PatreonService>();

        const int nonExistentShowId = -1;

        PatreonFeed feed = new("Test Feed", []);

        // Act & Assert
        await ThrowsAsync<InvalidOperationException>(() =>
            patreonService.IngestFeed(nonExistentShowId, feed, cancellationToken));
    }

    [Fact]
    public async Task IngestFeed_WhenFeedIsNew_CreatesPatreonPosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededDbContext(cancellationToken);

        List<PatreonPost> posts = [CreatePatreonPost(1), CreatePatreonPost(2)];

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
        Equal(posts[0].Id, postEntity1.PatreonId);
        Equal(posts[0].Title, postEntity1.Title);
        Equal(posts[0].Published, postEntity1.Published);
        Equal(posts[0].Summary, postEntity1.Summary);
        Equal(posts[0].Link, postEntity1.Link);
        Equal(posts[0].AudioUrl, postEntity1.AudioUrl);

        PatreonPostEntity postEntity2 = createdPostEntities[1];
        Equal(showEntity.Id, postEntity2.ShowId);
        Equal(posts[1].Id, postEntity2.PatreonId);
        Equal(posts[1].Title, postEntity2.Title);
        Equal(posts[1].Published, postEntity2.Published);
        Equal(posts[1].Summary, postEntity2.Summary);
        Equal(posts[1].Link, postEntity2.Link);
        Equal(posts[1].AudioUrl, postEntity2.AudioUrl);
    }

    [Fact]
    public async Task IngestFeed_WhenFeedHasExistingPosts_DoesNotCreateDuplicatePosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededDbContext(cancellationToken);

        PatreonPostEntity existingPostEntity = CreatePatreonPostEntity(showEntity.Id, patreonId: 1);

        await dbContext.PatreonPosts.AddAsync(existingPostEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PatreonPost> posts = [CreatePatreonPost(1), CreatePatreonPost(2)];

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
        Equal(existingPostEntity.PatreonId, postEntity1.PatreonId);
        Equal(existingPostEntity.Title, postEntity1.Title);
        Equal(existingPostEntity.Published, postEntity1.Published);
        Equal(existingPostEntity.Summary, postEntity1.Summary);
        Equal(existingPostEntity.Link, postEntity1.Link);
        Equal(existingPostEntity.AudioUrl, postEntity1.AudioUrl);

        PatreonPostEntity postEntity2 = postEntities[1];
        Equal(showEntity.Id, postEntity2.ShowId);
        Equal(posts[1].Id, postEntity2.PatreonId);
        Equal(posts[1].Title, postEntity2.Title);
        Equal(posts[1].Published, postEntity2.Published);
        Equal(posts[1].Summary, postEntity2.Summary);
        Equal(posts[1].Link, postEntity2.Link);
        Equal(posts[1].AudioUrl, postEntity2.AudioUrl);
    }

    [Fact]
    public async Task GetPendingPosts_WhenShowDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        PatreonService patreonService = Get<PatreonService>();

        const int nonExistentShowId = -1;

        // Act & Assert
        await ThrowsAsync<InvalidOperationException>(() =>
            patreonService.GetPendingPosts(nonExistentShowId, cancellationToken));
    }

    [Fact]
    public async Task GetPendingPosts_ShowHasPendingPostsWithNoProcessingError_ReturnsPendingPosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededDbContext(cancellationToken);

        PatreonPostEntity pendingPostEntity1 = CreatePatreonPostEntity(showEntity.Id, patreonId: 1);
        PatreonPostEntity pendingPostEntity2 = CreatePatreonPostEntity(showEntity.Id, patreonId: 2);

        await dbContext.PatreonPosts.AddRangeAsync([pendingPostEntity1, pendingPostEntity2], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonService patreonService = Get<PatreonService>();

        // Act
        ImmutableList<PendingPost> pendingPosts = await patreonService.GetPendingPosts(showEntity.Id, cancellationToken);

        // Assert
        NotNull(pendingPosts);
        Equal(2, pendingPosts.Count);

        PendingPost pendingPost1 = pendingPosts[0];
        Equal(pendingPostEntity1.Id, pendingPost1.Id);
        Equal(pendingPostEntity1.Title, pendingPost1.Title);
        Null(pendingPost1.ProcessingError);

        PendingPost pendingPost2 = pendingPosts[1];
        Equal(pendingPostEntity2.Id, pendingPost2.Id);
        Equal(pendingPostEntity2.Title, pendingPost2.Title);
        Null(pendingPost2.ProcessingError);
    }

    [Fact]
    public async Task GetPendingPosts_ShowHasPendingPostsWithProcessingErrors_ReturnsPendingPosts()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededDbContext(cancellationToken);

        PatreonPostEntity pendingPostEntity1 = CreatePatreonPostEntity(showEntity.Id, patreonId: 1, processingError: "Error processing post 1");
        PatreonPostEntity pendingPostEntity2 = CreatePatreonPostEntity(showEntity.Id, patreonId: 2, processingError: "Error processing post 2");

        await dbContext.PatreonPosts.AddRangeAsync([pendingPostEntity1, pendingPostEntity2], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonService patreonService = Get<PatreonService>();

        // Act
        ImmutableList<PendingPost> pendingPosts = await patreonService.GetPendingPosts(showEntity.Id, cancellationToken);

        // Assert
        NotNull(pendingPosts);
        Equal(2, pendingPosts.Count);

        PendingPost pendingPost1 = pendingPosts[0];
        Equal(pendingPostEntity1.Id, pendingPost1.Id);
        Equal(pendingPostEntity1.Title, pendingPost1.Title);
        Equal(pendingPostEntity1.ProcessingError, pendingPost1.ProcessingError);

        PendingPost pendingPost2 = pendingPosts[1];
        Equal(pendingPostEntity2.Id, pendingPost2.Id);
        Equal(pendingPostEntity2.Title, pendingPost2.Title);
        Equal(pendingPostEntity2.ProcessingError, pendingPost2.ProcessingError);
    }

    [Fact]
    public async Task GetPendingPosts_ShowHasNoPendingPosts_ReturnsEmptyList()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededDbContext(cancellationToken);

        PatreonPostEntity processedPostEntity = CreatePatreonPostEntity(showEntity.Id, patreonId: 1, episodeId: 1);

        await dbContext.PatreonPosts.AddAsync(processedPostEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonService patreonService = Get<PatreonService>();

        // Act
        ImmutableList<PendingPost> pendingPosts = await patreonService.GetPendingPosts(showEntity.Id, cancellationToken);

        // Assert
        NotNull(pendingPosts);
        Empty(pendingPosts);
    }

    private async Task<(LsmArchiveDbContext dbContext, ShowEntity showEntity)> GetSeededDbContext(CancellationToken cancellationToken)
    {
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity showEntity = CreateShowEntity("Test Show");
        await dbContext.Shows.AddAsync(showEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (dbContext, showEntity);
    }

    private static PatreonPost CreatePatreonPost(int id) =>
        new(
            Id: id,
            Title: $"Test Post {id}",
            Published: DateTimeOffset.UnixEpoch.AddDays(id),
            Summary: $"Summary of Test Post {id}",
            Link: $"https://www.patreon.com/posts/test-post-{id}",
            AudioUrl: $"https://www.patreon.com/posts/test-post-{id}/audio");

    private static PatreonPostEntity CreatePatreonPostEntity(int showId, int patreonId, string? processingError = null, int? episodeId = null) =>
        new()
        {
            ShowId = showId,
            PatreonId = patreonId,
            Title = $"Test Post {patreonId}",
            Published = DateTimeOffset.UnixEpoch.AddDays(patreonId),
            Summary = $"Summary of Test Post {patreonId}",
            Link = $"https://www.patreon.com/posts/test-post-{patreonId}",
            AudioUrl = $"https://www.patreon.com/posts/test-post-{patreonId}/audio",
            ProcessingError = processingError,
            EpisodeId = episodeId
        };

    private ShowEntity CreateShowEntity(string name) => new() { Name = name };
}
