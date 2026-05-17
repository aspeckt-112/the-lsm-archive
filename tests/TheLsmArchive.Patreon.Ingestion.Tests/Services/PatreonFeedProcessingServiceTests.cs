using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Services;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

public sealed class PatreonFeedProcessingServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreateShowAsync_WhenShowAlreadyExists_ReturnsExistingShowId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity existingShowEntity = new() { Name = "Test Show" };

        await dbContext.Shows.AddAsync(existingShowEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        int showId = await CreateService().GetOrCreateShowAsync(existingShowEntity.Name, cancellationToken);

        Equal(existingShowEntity.Id, showId);
    }

    [Fact]
    public async Task GetOrCreateShowAsync_WhenShowDoesNotExist_CreatesShowAndReturnsNewShowId()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        int showId = await CreateService().GetOrCreateShowAsync("New Test Show", cancellationToken);

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity? createdShowEntity = await dbContext.Shows
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == showId, cancellationToken);

        NotNull(createdShowEntity);
        Equal("New Test Show", createdShowEntity.Name);
        Equal(showId, createdShowEntity.Id);
    }

    [Fact]
    public async Task IngestFeedAsync_WhenShowDoesNotExist_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        const int nonExistentShowId = -1;

        PatreonFeed feed = new("Test Feed", []);

        await ThrowsAsync<InvalidOperationException>(() =>
            CreateService().IngestFeedAsync(nonExistentShowId, feed, cancellationToken));
    }

    [Fact]
    public async Task IngestFeedAsync_WhenFeedIsNew_CreatesPatreonPosts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededShowAsync(cancellationToken);

        List<PatreonPost> posts = [CreatePatreonPost(1), CreatePatreonPost(2)];

        PatreonFeed feed = new("Test Feed", posts);

        await CreateService().IngestFeedAsync(showEntity.Id, feed, cancellationToken);

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
    public async Task IngestFeedAsync_WhenFeedHasExistingPosts_DoesNotCreateDuplicatePosts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededShowAsync(cancellationToken);

        PatreonPostEntity existingPostEntity = CreatePatreonPostEntity(showEntity.Id, patreonId: 1);

        await dbContext.PatreonPosts.AddAsync(existingPostEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PatreonPost> posts = [CreatePatreonPost(1), CreatePatreonPost(2)];

        PatreonFeed feed = new("Test Feed", posts);

        await CreateService().IngestFeedAsync(showEntity.Id, feed, cancellationToken);

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
    public async Task GetPendingPostsAsync_WhenShowDoesNotExist_ThrowsInvalidOperationException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        const int nonExistentShowId = -1;

        await ThrowsAsync<InvalidOperationException>(() =>
            CreateService().GetPendingPostsAsync(nonExistentShowId, cancellationToken));
    }

    [Fact]
    public async Task GetPendingPostsAsync_WhenShowHasPendingAndProcessedPosts_ReturnsOnlyPendingPosts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity showEntity) = await GetSeededShowAsync(cancellationToken);

        PatreonPostEntity pendingPostEntity = CreatePatreonPostEntity(showEntity.Id, patreonId: 1);
        PatreonPostEntity failedPostEntity = CreatePatreonPostEntity(
            showEntity.Id,
            patreonId: 2,
            processingError: "Error processing post 2");

        await dbContext.PatreonPosts.AddRangeAsync([pendingPostEntity, failedPostEntity], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonPostEntity processedPostEntity = CreatePatreonPostEntity(showEntity.Id, patreonId: 3);
        await dbContext.PatreonPosts.AddAsync(processedPostEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        EpisodeEntity processedEpisode = new()
        {
            ShowId = showEntity.Id,
            Title = processedPostEntity.Title,
            ReleaseDateUtc = processedPostEntity.Published.UtcDateTime,
            PatreonPostId = processedPostEntity.Id
        };

        await dbContext.Episodes.AddAsync(processedEpisode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        processedPostEntity.EpisodeId = processedEpisode.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PendingPost> pendingPosts = [.. await CreateService().GetPendingPostsAsync(showEntity.Id, cancellationToken)];

        NotNull(pendingPosts);
        Equal(2, pendingPosts.Count);

        PendingPost pendingPost = pendingPosts.Single(p => p.Id == pendingPostEntity.Id);
        Equal(pendingPostEntity.Title, pendingPost.Title);
        Null(pendingPost.ProcessingError);

        PendingPost failedPost = pendingPosts.Single(p => p.Id == failedPostEntity.Id);
        Equal(failedPostEntity.Title, failedPost.Title);
        Equal(failedPostEntity.ProcessingError, failedPost.ProcessingError);
    }

    [Fact]
    public async Task ProcessPendingPostAsync_WhenSuccessful_CommitsEpisodePeopleTopicsRelationshipsAndPostState()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity show, PatreonPostEntity post) =
            await GetSeededPendingPostAsync(cancellationToken);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: ["Host One"],
                Guests: ["Guest One"],
                Topics: ["Topic One"]));

        await service.ProcessPendingPostAsync(show.Id, new PendingPost(post.Id, post.Title, null), cancellationToken);

        dbContext.ChangeTracker.Clear();

        EpisodeEntity? episode = await dbContext.Episodes
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        NotNull(episode);

        PatreonPostEntity? refreshedPost = await dbContext.PatreonPosts
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == post.Id, cancellationToken);

        NotNull(refreshedPost);
        Equal(episode.Id, refreshedPost.EpisodeId);
        Null(refreshedPost.ProcessingError);

        int personCount = await dbContext.Persons.CountAsync(cancellationToken);
        int topicCount = await dbContext.Topics.CountAsync(cancellationToken);
        int personEpisodeCount = await dbContext.PersonEpisodes.CountAsync(pe => pe.EpisodeId == episode.Id, cancellationToken);
        int topicEpisodeCount = await dbContext.TopicEpisodes.CountAsync(te => te.EpisodeId == episode.Id, cancellationToken);
        int personTopicCount = await dbContext.PersonTopics.CountAsync(cancellationToken);

        Equal(2, personCount);
        Equal(1, topicCount);
        Equal(2, personEpisodeCount);
        Equal(1, topicEpisodeCount);
        Equal(2, personTopicCount);
    }

    [Fact]
    public async Task ProcessPendingPostAsync_WhenWritePhaseFails_RollsBackImportAndPersistsProcessingError()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity show, PatreonPostEntity post) =
            await GetSeededPendingPostAsync(cancellationToken);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: [new string('A', 201)],
                Guests: [],
                Topics: ["Topic One"]));

        await ThrowsAnyAsync<DbUpdateException>(
            () => service.ProcessPendingPostAsync(show.Id, new PendingPost(post.Id, post.Title, null), cancellationToken));

        dbContext.ChangeTracker.Clear();

        int episodeCount = await dbContext.Episodes.CountAsync(e => e.PatreonPostId == post.Id, cancellationToken);
        int personCount = await dbContext.Persons.CountAsync(cancellationToken);
        int topicCount = await dbContext.Topics.CountAsync(cancellationToken);
        int personEpisodeCount = await dbContext.PersonEpisodes.CountAsync(cancellationToken);
        int topicEpisodeCount = await dbContext.TopicEpisodes.CountAsync(cancellationToken);
        int personTopicCount = await dbContext.PersonTopics.CountAsync(cancellationToken);

        PatreonPostEntity? refreshedPost = await dbContext.PatreonPosts
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == post.Id, cancellationToken);

        NotNull(refreshedPost);
        Null(refreshedPost.EpisodeId);
        False(string.IsNullOrWhiteSpace(refreshedPost.ProcessingError));
        Equal(0, episodeCount);
        Equal(0, personCount);
        Equal(0, topicCount);
        Equal(0, personEpisodeCount);
        Equal(0, topicEpisodeCount);
        Equal(0, personTopicCount);
    }

    [Fact]
    public async Task ProcessPendingPostAsync_WhenSummaryContainsDuplicateNames_DeduplicatesEntitiesAndLinks()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity show, PatreonPostEntity post) =
            await GetSeededPendingPostAsync(cancellationToken);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: ["Host One", " host one "],
                Guests: ["HOST-ONE"],
                Topics: ["Topic One", "topic one", "Topic-One"]));

        await service.ProcessPendingPostAsync(show.Id, new PendingPost(post.Id, post.Title, null), cancellationToken);

        dbContext.ChangeTracker.Clear();

        EpisodeEntity episode = await dbContext.Episodes
            .AsNoTracking()
            .SingleAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        Equal(1, await dbContext.Persons.CountAsync(cancellationToken));
        Equal(1, await dbContext.Topics.CountAsync(cancellationToken));
        Equal(1, await dbContext.PersonEpisodes.CountAsync(pe => pe.EpisodeId == episode.Id, cancellationToken));
        Equal(1, await dbContext.TopicEpisodes.CountAsync(te => te.EpisodeId == episode.Id, cancellationToken));
        Equal(1, await dbContext.PersonTopics.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ProcessPendingPostAsync_WhenReprocessed_ReusesEpisodeAndDoesNotDuplicateRelationships()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity show, PatreonPostEntity post) =
            await GetSeededPendingPostAsync(cancellationToken);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: ["Host One"],
                Guests: ["Guest One"],
                Topics: ["Topic One"]));

        PendingPost pendingPost = new(post.Id, post.Title, null);

        await service.ProcessPendingPostAsync(show.Id, pendingPost, cancellationToken);
        await service.ProcessPendingPostAsync(show.Id, pendingPost, cancellationToken);

        dbContext.ChangeTracker.Clear();

        EpisodeEntity episode = await dbContext.Episodes
            .AsNoTracking()
            .SingleAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        Equal(1, await dbContext.Episodes.CountAsync(e => e.PatreonPostId == post.Id, cancellationToken));
        Equal(2, await dbContext.Persons.CountAsync(cancellationToken));
        Equal(1, await dbContext.Topics.CountAsync(cancellationToken));
        Equal(2, await dbContext.PersonEpisodes.CountAsync(pe => pe.EpisodeId == episode.Id, cancellationToken));
        Equal(1, await dbContext.TopicEpisodes.CountAsync(te => te.EpisodeId == episode.Id, cancellationToken));
        Equal(2, await dbContext.PersonTopics.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ProcessPendingPostAsync_WhenNamesMatchExistingRows_ReusesExistingPersonsAndTopics()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, ShowEntity show, PatreonPostEntity post) =
            await GetSeededPendingPostAsync(cancellationToken);

        dbContext.Persons.Add(new PersonEntity { Name = "Alice Smith", NormalizedName = "alicesmith" });
        dbContext.Topics.Add(new TopicEntity { Name = "Dark Souls", NormalizedName = "darksouls" });
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: ["  Alice Smith  "],
                Guests: [],
                Topics: ["  Dark Souls  "]));

        await service.ProcessPendingPostAsync(show.Id, new PendingPost(post.Id, post.Title, null), cancellationToken);

        dbContext.ChangeTracker.Clear();

        Equal(1, await dbContext.Persons.CountAsync(cancellationToken));
        Equal(1, await dbContext.Topics.CountAsync(cancellationToken));
        Equal(1, await dbContext.PersonEpisodes.CountAsync(cancellationToken));
        Equal(1, await dbContext.TopicEpisodes.CountAsync(cancellationToken));
        Equal(1, await dbContext.PersonTopics.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task ProcessFeedAsync_WhenFeedContainsNewPost_CompletesEndToEnd()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        PatreonFeed feed = new(
            "Feed Driven Show",
            [CreatePatreonPost(1)]);

        PatreonFeedProcessingService service = CreateService(
            new AiSummary(
                Hosts: ["Host One"],
                Guests: [],
                Topics: ["Topic One"]));

        await service.ProcessFeedAsync(feed, cancellationToken);

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        dbContext.ChangeTracker.Clear();

        ShowEntity show = await dbContext.Shows
            .AsNoTracking()
            .SingleAsync(s => s.Name == feed.Title, cancellationToken);

        PatreonPostEntity post = await dbContext.PatreonPosts
            .AsNoTracking()
            .SingleAsync(p => p.ShowId == show.Id && p.PatreonId == 1, cancellationToken);

        EpisodeEntity episode = await dbContext.Episodes
            .AsNoTracking()
            .SingleAsync(e => e.PatreonPostId == post.Id, cancellationToken);

        Equal(show.Id, episode.ShowId);
        Equal(episode.Id, post.EpisodeId);
        Null(post.ProcessingError);
    }

    private PatreonFeedProcessingService CreateService(AiSummary? aiSummary = null)
    {
        return new PatreonFeedProcessingService(
            NullLogger<PatreonFeedProcessingService>.Instance,
            Get<IDbContextFactory<LsmArchiveDbContext>>(),
            new StubMetadataExtractionService(aiSummary ?? new AiSummary([], [], [])));
    }

    private async Task<(LsmArchiveDbContext dbContext, ShowEntity showEntity)> GetSeededShowAsync(
        CancellationToken cancellationToken)
    {
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity showEntity = new() { Name = "Test Show" };
        await dbContext.Shows.AddAsync(showEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (dbContext, showEntity);
    }

    private async Task<(LsmArchiveDbContext DbContext, ShowEntity Show, PatreonPostEntity Post)> GetSeededPendingPostAsync(
        CancellationToken cancellationToken)
    {
        (LsmArchiveDbContext dbContext, ShowEntity show) = await GetSeededShowAsync(cancellationToken);

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

        return (dbContext, show, post);
    }

    private static PatreonPost CreatePatreonPost(int id) =>
        new(
            Id: id,
            Title: $"Test Post {id}",
            Published: DateTimeOffset.UnixEpoch.AddDays(id),
            Summary: $"Summary of Test Post {id}",
            Link: $"https://www.patreon.com/posts/test-post-{id}",
            AudioUrl: $"https://www.patreon.com/posts/test-post-{id}/audio");

    private static PatreonPostEntity CreatePatreonPostEntity(
        int showId,
        int patreonId,
        string? processingError = null)
        => new()
        {
            ShowId = showId,
            PatreonId = patreonId,
            Title = $"Test Post {patreonId}",
            Published = DateTimeOffset.UnixEpoch.AddDays(patreonId),
            Summary = $"Summary of Test Post {patreonId}",
            Link = $"https://www.patreon.com/posts/test-post-{patreonId}",
            AudioUrl = $"https://www.patreon.com/posts/test-post-{patreonId}/audio",
            ProcessingError = processingError
        };

    private sealed class StubMetadataExtractionService(AiSummary aiSummary) : IMetadataExtractionService
    {
        public Task<AiSummary> ExtractMetadataAsync(
            ShowEntity show,
            PatreonPostEntity patreonPost,
            CancellationToken cancellationToken,
            IList<string>? knownPersons = null,
            IList<string>? knownTopics = null)
            => Task.FromResult(aiSummary);
    }
}

