using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Infrastructure;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Tests.Services.Database;

public sealed class RelationshipServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task LinkPersonsToEpisode_WhenNew_AddsPersonEpisodeLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, EpisodeEntity episode, List<PersonEntity> persons, _) =
            await GetSeededDbContext(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act
        await relationshipService.LinkPersonsToEpisodeAsync(persons, episode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        List<PersonEpisodeEntity> links = await dbContext.PersonEpisodes
            .Where(pe => pe.EpisodeId == episode.Id)
            .ToListAsync(cancellationToken);

        Equal(2, links.Count);
        Contains(links, l => l.PersonId == persons[0].Id);
        Contains(links, l => l.PersonId == persons[1].Id);
    }

    [Fact]
    public async Task LinkPersonsToEpisode_WhenAlreadyLinked_DoesNotCreateDuplicateLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, EpisodeEntity episode, List<PersonEntity> persons, _) =
            await GetSeededDbContext(cancellationToken);

        dbContext.PersonEpisodes.Add(new PersonEpisodeEntity
        {
            PersonId = persons[0].Id,
            EpisodeId = episode.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act — link the same persons again (retry scenario)
        await relationshipService.LinkPersonsToEpisodeAsync(persons, episode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert — person[0] link already existed; person[1] is new
        int count = await dbContext.PersonEpisodes
            .CountAsync(pe => pe.EpisodeId == episode.Id, cancellationToken);

        Equal(2, count);
    }

    [Fact]
    public async Task LinkTopicsToEpisode_WhenNew_AddsTopicEpisodeLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, EpisodeEntity episode, _, List<TopicEntity> topics) =
            await GetSeededDbContext(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act
        await relationshipService.LinkTopicsToEpisodeAsync(topics, episode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        List<TopicEpisodeEntity> links = await dbContext.TopicEpisodes
            .Where(te => te.EpisodeId == episode.Id)
            .ToListAsync(cancellationToken);

        Equal(2, links.Count);
        Contains(links, l => l.TopicId == topics[0].Id);
        Contains(links, l => l.TopicId == topics[1].Id);
    }

    [Fact]
    public async Task LinkTopicsToEpisode_WhenAlreadyLinked_DoesNotCreateDuplicateLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, EpisodeEntity episode, _, List<TopicEntity> topics) =
            await GetSeededDbContext(cancellationToken);

        dbContext.TopicEpisodes.Add(new TopicEpisodeEntity
        {
            TopicId = topics[0].Id,
            EpisodeId = episode.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act — link the same topics again (retry scenario)
        await relationshipService.LinkTopicsToEpisodeAsync(topics, episode, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert — topics[0] already existed; topics[1] is new
        int count = await dbContext.TopicEpisodes
            .CountAsync(te => te.EpisodeId == episode.Id, cancellationToken);

        Equal(2, count);
    }

    [Fact]
    public async Task LinkPersonsToTopics_WhenNew_AddsPersonTopicLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, _, List<PersonEntity> persons, List<TopicEntity> topics) =
            await GetSeededDbContext(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act
        await relationshipService.LinkPersonsToTopicsAsync(persons, topics, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert — 2 persons × 2 topics = 4 links
        int count = await dbContext.PersonTopics
            .CountAsync(
                pt => persons.Select(p => p.Id).Contains(pt.PersonId)
                   && topics.Select(t => t.Id).Contains(pt.TopicId),
                cancellationToken);

        Equal(4, count);
    }

    [Fact]
    public async Task LinkPersonsToTopics_WhenAlreadyLinked_DoesNotCreateDuplicateLinks()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        (LsmArchiveDbContext dbContext, _, List<PersonEntity> persons, List<TopicEntity> topics) =
            await GetSeededDbContext(cancellationToken);

        // Seed one existing link
        dbContext.PersonTopics.Add(new PersonTopicEntity
        {
            PersonId = persons[0].Id,
            TopicId = topics[0].Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        RelationshipService relationshipService = Get<RelationshipService>();

        // Act — link the full cross-product again (retry scenario)
        await relationshipService.LinkPersonsToTopicsAsync(persons, topics, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert — 2 × 2 = 4 total, no duplicates
        int count = await dbContext.PersonTopics
            .CountAsync(
                pt => persons.Select(p => p.Id).Contains(pt.PersonId)
                   && topics.Select(t => t.Id).Contains(pt.TopicId),
                cancellationToken);

        Equal(4, count);
    }

    private async Task<(LsmArchiveDbContext dbContext, EpisodeEntity episode, List<PersonEntity> persons, List<TopicEntity> topics)>
        GetSeededDbContext(CancellationToken cancellationToken)
    {
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity show = new() { Name = "Test Show" };
        dbContext.Shows.Add(show);
        await dbContext.SaveChangesAsync(cancellationToken);

        PatreonPostEntity post = new()
        {
            ShowId = show.Id,
            PatreonId = 1,
            Title = "Test Post",
            Published = DateTimeOffset.UnixEpoch.AddDays(1),
            Summary = "A test post",
            Link = "https://www.patreon.com/posts/test-1",
            AudioUrl = "https://www.patreon.com/posts/test-1/audio"
        };

        dbContext.PatreonPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        EpisodeEntity episode = new()
        {
            ShowId = show.Id,
            Title = "Test Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPostId = post.Id
        };

        dbContext.Episodes.Add(episode);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<PersonEntity> persons =
        [
            new() { Name = "Person One", NormalizedName = "personone" },
            new() { Name = "Person Two", NormalizedName = "persontwo" }
        ];

        dbContext.Persons.AddRange(persons);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<TopicEntity> topics =
        [
            new() { Name = "Topic One", NormalizedName = "topicone" },
            new() { Name = "Topic Two", NormalizedName = "topictwo" }
        ];

        dbContext.Topics.AddRange(topics);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (dbContext, episode, persons, topics);
    }
}
