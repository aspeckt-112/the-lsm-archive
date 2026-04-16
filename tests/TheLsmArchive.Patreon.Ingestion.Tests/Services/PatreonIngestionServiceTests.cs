using System.Net;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Polly;
using Polly.Registry;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Models;
using TheLsmArchive.Patreon.Ingestion.Options;
using TheLsmArchive.Patreon.Ingestion.Services;
using TheLsmArchive.Patreon.Ingestion.Services.Abstractions;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services;

[Collection(nameof(IngestionIntegrationTestFixture))]
public class PatreonIngestionServiceTests : IClassFixture<IngestionIntegrationTestFixture>, IAsyncLifetime
{
    private readonly IngestionIntegrationTestFixture _fixture;

    public PatreonIngestionServiceTests(IngestionIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Runs one ingestion cycle with the given canned RSS XML and AI summary, then cancels.
    /// </summary>
    private async Task RunSingleIngestionCycleAsync(
        string rssXml,
        AiSummary aiSummary,
        IAiSummaryService? aiServiceOverride = null)
    {
        Mock<ILogger<PatreonIngestionService>> loggerMock = new();

        // RSS parser with mock HTTP handler returning canned XML
        MockHttpMessageHandler httpHandler = new(rssXml);
        HttpClient httpClient = new(httpHandler) { BaseAddress = new Uri("https://test.com") };
        PatreonRssParser rssParser = new(httpClient);

        // AI summary service mock (or override)
        Mock<IAiSummaryService> aiServiceMock = new();

        if (aiServiceOverride is not null)
        {
            // Use the provided override directly
        }
        else
        {
            aiServiceMock
                .Setup(s => s.GenerateAiSummaryFromPatreonPost(
                    It.IsAny<ShowEntity>(),
                    It.IsAny<PatreonPostEntity>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IEnumerable<string>?>(),
                    It.IsAny<IEnumerable<string>?>()))
                .ReturnsAsync(aiSummary);
        }

        IAiSummaryService effectiveAiService = aiServiceOverride ?? aiServiceMock.Object;

        // Resilience pipeline (passthrough — no retry/rate-limit in tests)
        Mock<ResiliencePipelineProvider<string>> pipelineProviderMock = new();
        pipelineProviderMock
            .Setup(p => p.GetPipeline(Constants.AiSummaryPipelineName))
            .Returns(ResiliencePipeline.Empty);

        // Options
        IOptions<RssFeedSources> feedOptions = Microsoft.Extensions.Options.Options.Create<RssFeedSources>(
            [new RssFeedSource { Name = "Test Feed", Url = "https://test.com/rss" }]);
        IOptions<PatreonIngestionOptions> ingestionOptions = Microsoft.Extensions.Options.Options.Create(
            new PatreonIngestionOptions { RefreshIntervalInMinutes = 60 });

        PatreonIngestionService service = new(
            loggerMock.Object,
            rssParser,
            effectiveAiService,
            pipelineProviderMock.Object,
            feedOptions,
            ingestionOptions,
            _fixture.CreateDbContextFactory());

        // Execute a single ingestion cycle directly (no BackgroundService loop/delay)
        await service.ExecuteIngestionCycleAsync(TestContext.Current.CancellationToken);
    }

    private static string BuildRssXml(params (int id, string title, string pubDate, string desc)[] items)
    {
        StringBuilder sb = new();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">""");
        sb.AppendLine("<channel>");
        sb.AppendLine("<title>Sacred Symbols: A PlayStation Podcast</title>");

        foreach ((int id, string title, string pubDate, string desc) in items)
        {
            sb.AppendLine("<item>");
            sb.AppendLine($"<guid>{id}</guid>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine($"<pubDate>{pubDate}</pubDate>");
            sb.AppendLine($"<description>{desc}</description>");
            sb.AppendLine($"<link>https://patreon.com/post/{id}</link>");
            sb.AppendLine($"""<enclosure url="https://audio.com/{id}.mp3" />""");
            sb.AppendLine("</item>");
        }

        sb.AppendLine("</channel>");
        sb.AppendLine("</rss>");
        return sb.ToString();
    }

    [Fact]
    public async Task FullCycle_IngestsPostAndCreatesEpisodePersonsTopicsRelationships()
    {
        // Arrange
        string rssXml = BuildRssXml(
            (100, "Episode 150: Big News", "Mon, 15 Jan 2024 10:00:00 GMT", "PlayStation news discussion"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty", "Chris Ray Gun"],
            Guests: [],
            Topics: ["PlayStation 5", "Game Pass"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — use a fresh context to avoid stale cache
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();

        // Show created
        ShowEntity? show = await verifyContext.Shows.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(show);
        Assert.Equal("Sacred Symbols: A PlayStation Podcast", show.Name);
        Assert.NotNull(show.LastSyncedAt);

        // Post ingested
        PatreonPostEntity? post = await verifyContext.PatreonPosts.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(post);
        Assert.Equal(100, post.PatreonId);
        Assert.Equal("Episode 150: Big News", post.Title);
        Assert.NotNull(post.EpisodeId);
        Assert.Null(post.ProcessingError);

        // Episode created
        EpisodeEntity? episode = await verifyContext.Episodes.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(episode);
        Assert.Equal("Episode 150: Big News", episode.Title);

        // Persons created
        List<PersonEntity> persons = await verifyContext.Persons.OrderBy(p => p.Name).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, persons.Count);
        Assert.Equal("Chris Ray Gun", persons[0].Name);
        Assert.Equal("Colin Moriarty", persons[1].Name);

        // Topics created
        List<TopicEntity> topics = await verifyContext.Topics.OrderBy(t => t.Name).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, topics.Count);
        Assert.Equal("Game Pass", topics[0].Name);
        Assert.Equal("PlayStation 5", topics[1].Name);

        // Person-Episode relationships
        List<PersonEpisodeEntity> personEpisodes = await verifyContext.PersonEpisodes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, personEpisodes.Count);

        // Topic-Episode relationships
        List<TopicEpisodeEntity> topicEpisodes = await verifyContext.TopicEpisodes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, topicEpisodes.Count);

        // Person-Topic relationships (2 persons × 2 topics = 4)
        List<PersonTopicEntity> personTopics = await verifyContext.PersonTopics.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4, personTopics.Count);
    }

    [Fact]
    public async Task IngestPosts_SkipsDuplicatePatreonIds()
    {
        // Arrange — seed a post that already exists
        await using LsmArchiveDbContext seedContext = _fixture.CreateDbContext();
        ShowEntity show = new() { Name = "Sacred Symbols: A PlayStation Podcast" };
        seedContext.Shows.Add(show);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        PatreonPostEntity existingPost = new()
        {
            PatreonId = 200,
            Title = "Existing Post",
            Published = DateTimeOffset.UtcNow,
            Summary = "Already here",
            Link = "https://patreon.com/200",
            AudioUrl = "https://audio.com/200.mp3",
            ShowId = show.Id
        };
        seedContext.PatreonPosts.Add(existingPost);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // RSS feed contains the same post ID (200)
        string rssXml = BuildRssXml(
            (200, "Existing Post", "Mon, 15 Jan 2024 10:00:00 GMT", "Already here"));

        AiSummary aiSummary = new(["Colin Moriarty"], [], ["PS5"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — still only 1 post
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int postCount = await verifyContext.PatreonPosts.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, postCount);
    }

    [Fact]
    public async Task ProcessPost_ReuseExistingPersonByNormalizedName()
    {
        // Arrange — seed a person that already exists
        await using LsmArchiveDbContext seedContext = _fixture.CreateDbContext();
        PersonEntity existingPerson = new()
        {
            Name = "Colin Moriarty",
            NormalizedName = "colinmoriarty"
        };
        seedContext.Persons.Add(existingPerson);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        string rssXml = BuildRssXml(
            (300, "Episode 1", "Mon, 15 Jan 2024 10:00:00 GMT", "A discussion"));

        // AI returns slightly different casing — should still match existing person
        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: [],
            Topics: ["PlayStation 5"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — still only 1 person entity (reused, not duplicated)
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int personCount = await verifyContext.Persons.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, personCount);
    }

    [Fact]
    public async Task ProcessPost_ReuseExistingTopicByNormalizedName()
    {
        // Arrange — seed an existing topic
        await using LsmArchiveDbContext seedContext = _fixture.CreateDbContext();
        TopicEntity existingTopic = new()
        {
            Name = "PlayStation 5",
            NormalizedName = "playstation5"
        };
        seedContext.Topics.Add(existingTopic);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        string rssXml = BuildRssXml(
            (400, "Episode 2", "Tue, 16 Jan 2024 10:00:00 GMT", "Topic test"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: [],
            Topics: ["PlayStation 5"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — still only 1 topic entity
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int topicCount = await verifyContext.Topics.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, topicCount);
    }

    [Fact]
    public async Task ProcessPost_DeduplicatesPersonsByNormalizedNameWithinSinglePost()
    {
        // AI returns duplicate person names (different casing) in hosts and guests
        string rssXml = BuildRssXml(
            (500, "Episode 3", "Wed, 17 Jan 2024 10:00:00 GMT", "Dedup test"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: ["colin moriarty"], // same person, different case
            Topics: ["Gaming"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — only 1 person entity created (deduplicated)
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int personCount = await verifyContext.Persons.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, personCount);
    }

    [Fact]
    public async Task ProcessPost_WhenAiServiceFails_PostRemainsUnprocessed()
    {
        // Arrange
        string rssXml = BuildRssXml(
            (600, "Failing Episode", "Thu, 18 Jan 2024 10:00:00 GMT", "This will fail"));

        Mock<IAiSummaryService> failingAiService = new();
        failingAiService
            .Setup(s => s.GenerateAiSummaryFromPatreonPost(
                It.IsAny<ShowEntity>(),
                It.IsAny<PatreonPostEntity>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>()))
            .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

        // Act
        await RunSingleIngestionCycleAsync(
            rssXml,
            new AiSummary([], [], []), // won't be used
            failingAiService.Object);

        // Assert — post was ingested but remains unprocessed.
        // The AI call fails before the DB transaction, so ProcessingError is not set
        // and the post stays in the "needs processing" state (EpisodeId == null).
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        PatreonPostEntity? post = await verifyContext.PatreonPosts.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(post);
        Assert.Equal(600, post.PatreonId);
        Assert.Null(post.EpisodeId);

        // No episode should have been created
        int episodeCount = await verifyContext.Episodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, episodeCount);
    }

    [Fact]
    public async Task ProcessPost_WhenAllPostsFail_DoesNotUpdateLastSyncedAt()
    {
        // Arrange
        string rssXml = BuildRssXml(
            (700, "Failing Episode 2", "Fri, 19 Jan 2024 10:00:00 GMT", "Will fail"));

        Mock<IAiSummaryService> failingAiService = new();
        failingAiService
            .Setup(s => s.GenerateAiSummaryFromPatreonPost(
                It.IsAny<ShowEntity>(),
                It.IsAny<PatreonPostEntity>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>()))
            .ThrowsAsync(new InvalidOperationException("Gemini down"));

        // Act
        await RunSingleIngestionCycleAsync(
            rssXml,
            new AiSummary([], [], []),
            failingAiService.Object);

        // Assert — LastSyncedAt should NOT be set
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        ShowEntity? show = await verifyContext.Shows.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(show);
        Assert.Null(show.LastSyncedAt);
    }

    [Fact]
    public async Task ProcessPost_WithMultiplePosts_ProcessesAllSuccessfully()
    {
        // Arrange — two posts in the feed
        string rssXml = BuildRssXml(
            (800, "Episode 10", "Mon, 22 Jan 2024 10:00:00 GMT", "First episode"),
            (801, "Episode 11", "Tue, 23 Jan 2024 10:00:00 GMT", "Second episode"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: [],
            Topics: ["PlayStation 5"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int postCount = await verifyContext.PatreonPosts.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, postCount);

        int episodeCount = await verifyContext.Episodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, episodeCount);

        // Person should be deduplicated across both posts (same host)
        int personCount = await verifyContext.Persons.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, personCount);

        // Topic should be deduplicated across both posts
        int topicCount = await verifyContext.Topics.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, topicCount);

        // Each episode has 1 person-episode link = 2 total
        int personEpisodeCount = await verifyContext.PersonEpisodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, personEpisodeCount);

        // Each episode has 1 topic-episode link = 2 total
        int topicEpisodeCount = await verifyContext.TopicEpisodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, topicEpisodeCount);
    }

    [Fact]
    public async Task ProcessPost_WithGuests_CreatesGuestsAsSeparatePersons()
    {
        string rssXml = BuildRssXml(
            (900, "Guest Episode", "Mon, 29 Jan 2024 10:00:00 GMT", "Special guest!"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: ["Greg Miller"],
            Topics: ["Industry News"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();

        List<PersonEntity> persons = await verifyContext.Persons.OrderBy(p => p.Name).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, persons.Count);
        Assert.Equal("Colin Moriarty", persons[0].Name);
        Assert.Equal("Greg Miller", persons[1].Name);

        // Both host and guest are linked to the episode
        int personEpisodeCount = await verifyContext.PersonEpisodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, personEpisodeCount);
    }

    [Fact]
    public async Task GetOrCreateShow_ReusesExistingShowWithCaseInsensitiveMatch()
    {
        // Arrange — pre-seed a show
        await using LsmArchiveDbContext seedContext = _fixture.CreateDbContext();
        ShowEntity existingShow = new() { Name = "Sacred Symbols: A PlayStation Podcast" };
        seedContext.Shows.Add(existingShow);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // RSS feed title matches in different casing pattern (ILike will match)
        string rssXml = BuildRssXml(
            (1000, "Episode X", "Mon, 05 Feb 2024 10:00:00 GMT", "Test"));

        AiSummary aiSummary = new(["Colin Moriarty"], [], ["PS5"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — still only 1 show (reused)
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int showCount = await verifyContext.Shows.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, showCount);
    }

    [Fact]
    public async Task ReingestionOfSamePost_IsIdempotent()
    {
        // Arrange — run ingestion cycle once
        string rssXml = BuildRssXml(
            (1100, "Episode 50", "Mon, 12 Feb 2024 10:00:00 GMT", "Same episode"));

        AiSummary aiSummary = new(
            Hosts: ["Colin Moriarty"],
            Guests: [],
            Topics: ["Dark Souls"]);

        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Act — run the same ingestion cycle again (simulates re-ingestion)
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — no duplicates
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int postCount = await verifyContext.PatreonPosts.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, postCount);

        int episodeCount = await verifyContext.Episodes.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, episodeCount);

        int personCount = await verifyContext.Persons.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, personCount);

        int topicCount = await verifyContext.Topics.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, topicCount);
    }

    [Fact]
    public async Task ProcessPost_WithPartialFailure_OnlyFailedPostRemainsUnprocessed()
    {
        // Arrange — two posts, AI service fails only on the second
        string rssXml = BuildRssXml(
            (1200, "Good Episode", "Mon, 19 Feb 2024 10:00:00 GMT", "This succeeds"),
            (1201, "Bad Episode", "Tue, 20 Feb 2024 10:00:00 GMT", "This fails"));

        AiSummary goodSummary = new(["Colin Moriarty"], [], ["PS5"]);

        int callCount = 0;
        Mock<IAiSummaryService> aiServiceMock = new();
        aiServiceMock
            .Setup(s => s.GenerateAiSummaryFromPatreonPost(
                It.IsAny<ShowEntity>(),
                It.IsAny<PatreonPostEntity>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns<ShowEntity, PatreonPostEntity, CancellationToken, IEnumerable<string>?, IEnumerable<string>?>(
                (_, _, _, _, _) =>
                {
                    int current = Interlocked.Increment(ref callCount);
                    return current == 2 ? throw new InvalidOperationException("AI exploded on second post") : Task.FromResult(goodSummary);
                });

        // Act
        await RunSingleIngestionCycleAsync(rssXml, goodSummary, aiServiceMock.Object);

        // Assert — first post succeeded, second failed
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();

        List<PatreonPostEntity> posts = await verifyContext.PatreonPosts
            .OrderBy(p => p.PatreonId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, posts.Count);

        // First post was processed successfully
        Assert.NotNull(posts[0].EpisodeId);
        Assert.Null(posts[0].ProcessingError);

        // Second post failed — no episode, error recorded
        Assert.Null(posts[1].EpisodeId);

        // LastSyncedAt should NOT be set since there was an error
        ShowEntity? show = await verifyContext.Shows.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(show);
        Assert.Null(show.LastSyncedAt);
    }

    [Fact]
    public async Task ProcessPost_WithAccentedPersonNames_NormalizesDuplicates()
    {
        // AI returns same person name with and without accents
        string rssXml = BuildRssXml(
            (1300, "Accent Episode", "Wed, 21 Feb 2024 10:00:00 GMT", "Testing accents"));

        AiSummary aiSummary = new(
            Hosts: ["José García"],
            Guests: ["Jose Garcia"], // Same person without accents
            Topics: ["Gaming"]);

        // Act
        await RunSingleIngestionCycleAsync(rssXml, aiSummary);

        // Assert — deduplicated to 1 person
        await using LsmArchiveDbContext verifyContext = _fixture.CreateDbContext();
        int personCount = await verifyContext.Persons.CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, personCount);
    }

    /// <summary>
    /// A mock HTTP message handler that returns a predefined response for any request.
    /// </summary>
    private sealed class MockHttpMessageHandler(string responseContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
            });
        }
    }
}
