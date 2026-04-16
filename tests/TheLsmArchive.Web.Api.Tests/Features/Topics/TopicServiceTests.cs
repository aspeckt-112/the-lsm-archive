using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests.Features.Topics;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class TopicServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly TopicService _topicService;

    public TopicServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<TopicService>> loggerMock = new();

        _topicService = new TopicService(
            loggerMock.Object,
            DbContext
        );
    }

    #region GetById

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetById_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetById_WithValidIdButNonExistentTopic_ReturnsNull()
    {
        // Arrange & Act
        Topic? topic = await _topicService.GetById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(topic);
    }

    [Fact]
    public async Task GetById_WithExistingTopic_ReturnsTopic()
    {
        // Arrange
        TopicEntity topicEntity = new() { Name = "Test Topic", NormalizedName = "testtopic" };
        await InsertSingleInstanceOfEntityAsync(topicEntity);

        // Act
        Topic? topic = await _topicService.GetById(topicEntity.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(topic);
        Assert.Equal(topicEntity.Id, topic.Id);
        Assert.Equal("Test Topic", topic.Name);
    }

    #endregion

    #region GetTimeline

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetTimeline_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetTimeline(id, new PagedItemRequest(), true, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetTimeline_WithNonExistentTopic_ReturnsNull()
    {
        // Arrange & Act
        TopicTimeline? timeline = await _topicService.GetTimeline(9999, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(timeline);
    }

    [Fact]
    public async Task GetTimeline_WithExistingTopic_ReturnsEntriesOrderedByDate()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity topic = new() { Name = "Test Topic", NormalizedName = "testtopic" };
        await InsertSingleInstanceOfEntityAsync(topic);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new()
        {
            PatreonId = 1,
            Title = "Post 1",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        PatreonPostEntity post2 = new()
        {
            PatreonId = 2,
            Title = "Post 2",
            Link = "https://patreon.com/2",
            Summary = "Summary 2",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/2",
            ShowId = show.Id
        };

        EpisodeEntity ep1 = new()
        {
            Title = "Later Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 8, 25, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity ep2 = new()
        {
            Title = "Earlier Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 2, 10, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        TopicEpisodeEntity te1 = new() { TopicId = topic.Id, EpisodeId = ep1.Id };
        TopicEpisodeEntity te2 = new() { TopicId = topic.Id, EpisodeId = ep2.Id };
        await InsertSingleInstanceOfEntityAsync(te1);
        await InsertSingleInstanceOfEntityAsync(te2);

        PersonEpisodeEntity pe1 = new() { PersonId = person.Id, EpisodeId = ep1.Id };
        await InsertSingleInstanceOfEntityAsync(pe1);

        // Act
        TopicTimeline? timeline = await _topicService.GetTimeline(topic.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(timeline);
        Assert.Equal(new DateOnly(2024, 2, 10), timeline.FirstDiscussed);
        Assert.Equal(new DateOnly(2024, 8, 25), timeline.LastDiscussed);
        Assert.Equal(2, timeline.Entries.Items.Count);
        Assert.Equal(2, timeline.Entries.TotalCount);
        Assert.Equal("Later Episode", timeline.Entries.Items[0].Title);
        Assert.Equal("Earlier Episode", timeline.Entries.Items[1].Title);
        Assert.Empty(timeline.Entries.Items[1].People);
        Assert.Single(timeline.Entries.Items[0].People);
        Assert.Equal("Test Person", timeline.Entries.Items[0].People[0].Name);
    }

    #endregion

    #region GetByEpisodeId

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetByEpisodeId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetByEpisodeId(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByEpisodeId_WithNoAssociatedTopics_ReturnsEmptyList()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post = new()
        {
            PatreonId = 1,
            Title = "Post 1",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        EpisodeEntity episode = new()
        {
            Title = "Episode 1",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post,
            ShowId = show.Id
        };
        await InsertSingleInstanceOfEntityAsync(episode);

        // Act
        List<Topic> topics = await _topicService.GetByEpisodeId(episode.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(topics);
    }

    [Fact]
    public async Task GetByEpisodeId_WithAssociatedTopics_ReturnsTopicList()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity topic1 = new() { Name = "Topic A", NormalizedName = "topica" };
        TopicEntity topic2 = new() { Name = "Topic B", NormalizedName = "topicb" };
        await InsertSingleInstanceOfEntityAsync(topic1);
        await InsertSingleInstanceOfEntityAsync(topic2);

        PatreonPostEntity post = new()
        {
            PatreonId = 1,
            Title = "Post 1",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        EpisodeEntity episode = new()
        {
            Title = "Episode 1",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post,
            ShowId = show.Id
        };
        await InsertSingleInstanceOfEntityAsync(episode);

        TopicEpisodeEntity te1 = new() { TopicId = topic1.Id, EpisodeId = episode.Id };
        TopicEpisodeEntity te2 = new() { TopicId = topic2.Id, EpisodeId = episode.Id };
        await InsertSingleInstanceOfEntityAsync(te1);
        await InsertSingleInstanceOfEntityAsync(te2);

        // Act
        List<Topic> topics = await _topicService.GetByEpisodeId(episode.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, topics.Count);
        Assert.Contains(topics, t => t.Name == "Topic A");
        Assert.Contains(topics, t => t.Name == "Topic B");
    }

    [Fact]
    public async Task GetByEpisodeId_WithAssociatedTopics_ReturnsSortedAlphabetically()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity topicC = new() { Name = "Zulu Topic", NormalizedName = "zulutopic" };
        TopicEntity topicA = new() { Name = "Alpha Topic", NormalizedName = "alphatopic" };
        TopicEntity topicB = new() { Name = "Mango Topic", NormalizedName = "mangotopic" };
        await InsertSingleInstanceOfEntityAsync(topicC);
        await InsertSingleInstanceOfEntityAsync(topicA);
        await InsertSingleInstanceOfEntityAsync(topicB);

        PatreonPostEntity post = new()
        {
            PatreonId = 1,
            Title = "Post 1",
            Link = "https://patreon.com/1",
            Summary = "Summary 1",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/1",
            ShowId = show.Id
        };
        EpisodeEntity episode = new()
        {
            Title = "Episode 1",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post,
            ShowId = show.Id
        };
        await InsertSingleInstanceOfEntityAsync(episode);

        // Insert in non-alphabetical order to verify sorting
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topicC.Id, EpisodeId = episode.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topicA.Id, EpisodeId = episode.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topicB.Id, EpisodeId = episode.Id });

        // Act
        List<Topic> topics = await _topicService.GetByEpisodeId(episode.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, topics.Count);
        Assert.Equal("Alpha Topic", topics[0].Name);
        Assert.Equal("Mango Topic", topics[1].Name);
        Assert.Equal("Zulu Topic", topics[2].Name);
    }

    #endregion

    #region GetMostDiscussedByPersonId

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetMostDiscussedByPersonId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetMostDiscussedByPersonId(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetMostDiscussedByPersonId_WithAssociatedTopics_ReturnsRankedTopics()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity alpha = new() { Name = "Alpha Topic", NormalizedName = "alphatopic" };
        TopicEntity beta = new() { Name = "Beta Topic", NormalizedName = "betatopic" };
        TopicEntity gamma = new() { Name = "Gamma Topic", NormalizedName = "gammatopic" };
        await InsertSingleInstanceOfEntityAsync(alpha);
        await InsertSingleInstanceOfEntityAsync(beta);
        await InsertSingleInstanceOfEntityAsync(gamma);

        EpisodeEntity episode1 = await CreateEpisodeAsync(show.Id, 7001, "Episode A");
        EpisodeEntity episode2 = await CreateEpisodeAsync(show.Id, 7002, "Episode B");
        EpisodeEntity episode3 = await CreateEpisodeAsync(show.Id, 7003, "Episode C");

        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = episode1.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = episode2.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = episode3.Id });

        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = alpha.Id, EpisodeId = episode1.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = alpha.Id, EpisodeId = episode2.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = beta.Id, EpisodeId = episode1.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = gamma.Id, EpisodeId = episode3.Id });

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedByPersonId(
            person.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha Topic", result[0].Name);
        Assert.Equal(2, result[0].EpisodeCount);
        Assert.Equal("Beta Topic", result[1].Name);
        Assert.Equal(1, result[1].EpisodeCount);
        Assert.Equal("Gamma Topic", result[2].Name);
        Assert.Equal(1, result[2].EpisodeCount);
    }

    [Fact]
    public async Task GetMostDiscussedByPersonId_WithMoreThanTwentyFiveTopics_ReturnsTopTwentyFive()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        for (int index = 1; index <= 26; index++)
        {
            TopicEntity topic = new() { Name = $"Topic {index:D2}", NormalizedName = $"topic{index:D2}" };
            await InsertSingleInstanceOfEntityAsync(topic);

            EpisodeEntity episode = await CreateEpisodeAsync(show.Id, 7100 + index, $"Episode {index:D2}");

            await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = episode.Id });
            await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topic.Id, EpisodeId = episode.Id });
        }

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedByPersonId(
            person.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(25, result.Count);
        Assert.DoesNotContain(result, topic => topic.Name == "Topic 26");
    }

    #endregion

    #region GetMostDiscussedAlongsideByTopicId

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetMostDiscussedAlongsideByTopicId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetMostDiscussedAlongsideByTopicId(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetMostDiscussedAlongsideByTopicId_WithNoCoOccurringTopics_ReturnsEmptyList()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity topic = new() { Name = "Lonely Topic", NormalizedName = "lonelytopic" };
        await InsertSingleInstanceOfEntityAsync(topic);

        EpisodeEntity episode = await CreateEpisodeAsync(show.Id, 8001, "Solo Episode");
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topic.Id, EpisodeId = episode.Id });

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedAlongsideByTopicId(
            topic.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMostDiscussedAlongsideByTopicId_WithCoOccurringTopics_ReturnsRankedTopics()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity target = new() { Name = "Target Topic", NormalizedName = "targettopic" };
        TopicEntity alpha = new() { Name = "Alpha Topic", NormalizedName = "alphatopic" };
        TopicEntity beta = new() { Name = "Beta Topic", NormalizedName = "betatopic" };
        TopicEntity gamma = new() { Name = "Gamma Topic", NormalizedName = "gammatopic" };
        await InsertSingleInstanceOfEntityAsync(target);
        await InsertSingleInstanceOfEntityAsync(alpha);
        await InsertSingleInstanceOfEntityAsync(beta);
        await InsertSingleInstanceOfEntityAsync(gamma);

        EpisodeEntity episode1 = await CreateEpisodeAsync(show.Id, 8010, "Episode A");
        EpisodeEntity episode2 = await CreateEpisodeAsync(show.Id, 8011, "Episode B");
        EpisodeEntity episode3 = await CreateEpisodeAsync(show.Id, 8012, "Episode C");

        // Target appears in all 3 episodes
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = target.Id, EpisodeId = episode1.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = target.Id, EpisodeId = episode2.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = target.Id, EpisodeId = episode3.Id });

        // Alpha co-occurs in 2 episodes
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = alpha.Id, EpisodeId = episode1.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = alpha.Id, EpisodeId = episode2.Id });

        // Beta co-occurs in 1 episode
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = beta.Id, EpisodeId = episode1.Id });

        // Gamma co-occurs in 1 episode
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = gamma.Id, EpisodeId = episode3.Id });

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedAlongsideByTopicId(
            target.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha Topic", result[0].Name);
        Assert.Equal(2, result[0].EpisodeCount);
        Assert.Equal("Beta Topic", result[1].Name);
        Assert.Equal(1, result[1].EpisodeCount);
        Assert.Equal("Gamma Topic", result[2].Name);
        Assert.Equal(1, result[2].EpisodeCount);
    }

    [Fact]
    public async Task GetMostDiscussedAlongsideByTopicId_DoesNotIncludeTargetTopic()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity target = new() { Name = "Target Topic", NormalizedName = "targettopic" };
        TopicEntity other = new() { Name = "Other Topic", NormalizedName = "othertopic" };
        await InsertSingleInstanceOfEntityAsync(target);
        await InsertSingleInstanceOfEntityAsync(other);

        EpisodeEntity episode = await CreateEpisodeAsync(show.Id, 8020, "Shared Episode");
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = target.Id, EpisodeId = episode.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = other.Id, EpisodeId = episode.Id });

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedAlongsideByTopicId(
            target.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain(result, t => t.Name == "Target Topic");
        Assert.Equal("Other Topic", result[0].Name);
    }

    [Fact]
    public async Task GetMostDiscussedAlongsideByTopicId_WithMoreThanTwentyFiveTopics_ReturnsTopTwentyFive()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity target = new() { Name = "Target Topic", NormalizedName = "targettopic" };
        await InsertSingleInstanceOfEntityAsync(target);

        EpisodeEntity episode = await CreateEpisodeAsync(show.Id, 8030, "Big Episode");
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = target.Id, EpisodeId = episode.Id });

        for (int index = 1; index <= 26; index++)
        {
            TopicEntity topic = new() { Name = $"Topic {index:D2}", NormalizedName = $"topic{index:D2}" };
            await InsertSingleInstanceOfEntityAsync(topic);
            await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topic.Id, EpisodeId = episode.Id });
        }

        // Act
        List<MostDiscussedTopic> result = await _topicService.GetMostDiscussedAlongsideByTopicId(
            target.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(25, result.Count);
    }

    #endregion

    #region GetByPersonId

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetByPersonId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetByPersonId(id, new PagedItemRequest(), false, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByPersonId_WithNullPagedRequest_ThrowsArgumentNullException()
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _topicService.GetByPersonId(1, null!, false, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByPersonId_WithNoAssociatedTopics_ReturnsEmptyPagedResponse()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        // Act
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetByPersonId_WithAssociatedTopics_ReturnsPagedTopics()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topic1 = new() { Name = "Alpha Topic", NormalizedName = "alphatopic" };
        TopicEntity topic2 = new() { Name = "Beta Topic", NormalizedName = "betatopic" };
        await InsertSingleInstanceOfEntityAsync(topic1);
        await InsertSingleInstanceOfEntityAsync(topic2);

        PersonTopicEntity pt1 = new() { PersonId = person.Id, TopicId = topic1.Id };
        PersonTopicEntity pt2 = new() { PersonId = person.Id, TopicId = topic2.Id };
        await InsertSingleInstanceOfEntityAsync(pt1);
        await InsertSingleInstanceOfEntityAsync(pt2);

        // Act
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alpha Topic", result.Items[0].Name);
        Assert.Equal("Beta Topic", result.Items[1].Name);
    }

    [Fact]
    public async Task GetByPersonId_WithSearchTerm_FiltersResults()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topic1 = new() { Name = "PlayStation 5", NormalizedName = "playstation5" };
        TopicEntity topic2 = new() { Name = "Xbox Series X", NormalizedName = "xboxseriesx" };
        await InsertSingleInstanceOfEntityAsync(topic1);
        await InsertSingleInstanceOfEntityAsync(topic2);

        PersonTopicEntity pt1 = new() { PersonId = person.Id, TopicId = topic1.Id };
        PersonTopicEntity pt2 = new() { PersonId = person.Id, TopicId = topic2.Id };
        await InsertSingleInstanceOfEntityAsync(pt1);
        await InsertSingleInstanceOfEntityAsync(pt2);

        // Act
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(SearchTerm: "PlayStation"), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("PlayStation 5", result.Items[0].Name);
    }

    [Fact]
    public async Task GetByPersonId_WithPaging_ReturnsCorrectPage()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topic1 = new() { Name = "Alpha", NormalizedName = "alpha" };
        TopicEntity topic2 = new() { Name = "Beta", NormalizedName = "beta" };
        TopicEntity topic3 = new() { Name = "Gamma", NormalizedName = "gamma" };
        await InsertSingleInstanceOfEntityAsync(topic1);
        await InsertSingleInstanceOfEntityAsync(topic2);
        await InsertSingleInstanceOfEntityAsync(topic3);

        PersonTopicEntity pt1 = new() { PersonId = person.Id, TopicId = topic1.Id };
        PersonTopicEntity pt2 = new() { PersonId = person.Id, TopicId = topic2.Id };
        PersonTopicEntity pt3 = new() { PersonId = person.Id, TopicId = topic3.Id };
        await InsertSingleInstanceOfEntityAsync(pt1);
        await InsertSingleInstanceOfEntityAsync(pt2);
        await InsertSingleInstanceOfEntityAsync(pt3);

        // Act — page 2, size 1 (ordered by name: Alpha, Beta, Gamma)
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(PageNumber: 2, PageSize: 1), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Beta", result.Items[0].Name);
    }

    [Fact]
    public async Task GetByPersonId_SortAscending_ReturnsAToZ()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topicC = new() { Name = "Zulu", NormalizedName = "zulu" };
        TopicEntity topicA = new() { Name = "Alpha", NormalizedName = "alpha" };
        TopicEntity topicB = new() { Name = "Mango", NormalizedName = "mango" };
        await InsertSingleInstanceOfEntityAsync(topicC);
        await InsertSingleInstanceOfEntityAsync(topicA);
        await InsertSingleInstanceOfEntityAsync(topicB);

        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicC.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicA.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicB.Id });

        // Act
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal("Alpha", result.Items[0].Name);
        Assert.Equal("Mango", result.Items[1].Name);
        Assert.Equal("Zulu", result.Items[2].Name);
    }

    [Fact]
    public async Task GetByPersonId_SortDescending_ReturnsZToA()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topicC = new() { Name = "Zulu", NormalizedName = "zulu" };
        TopicEntity topicA = new() { Name = "Alpha", NormalizedName = "alpha" };
        TopicEntity topicB = new() { Name = "Mango", NormalizedName = "mango" };
        await InsertSingleInstanceOfEntityAsync(topicC);
        await InsertSingleInstanceOfEntityAsync(topicA);
        await InsertSingleInstanceOfEntityAsync(topicB);

        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicC.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicA.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicB.Id });

        // Act
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal("Zulu", result.Items[0].Name);
        Assert.Equal("Mango", result.Items[1].Name);
        Assert.Equal("Alpha", result.Items[2].Name);
    }

    [Fact]
    public async Task GetByPersonId_SortDescendingWithPaging_ReturnsCorrectPage()
    {
        // Arrange
        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topicA = new() { Name = "Alpha", NormalizedName = "alpha" };
        TopicEntity topicB = new() { Name = "Beta", NormalizedName = "beta" };
        TopicEntity topicC = new() { Name = "Gamma", NormalizedName = "gamma" };
        await InsertSingleInstanceOfEntityAsync(topicA);
        await InsertSingleInstanceOfEntityAsync(topicB);
        await InsertSingleInstanceOfEntityAsync(topicC);

        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicA.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicB.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonTopicEntity { PersonId = person.Id, TopicId = topicC.Id });

        // Act — page 2, size 1 (descending: Gamma, Beta, Alpha)
        PagedResponse<Topic> result = await _topicService.GetByPersonId(
            person.Id, new PagedItemRequest(PageNumber: 2, PageSize: 1), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Beta", result.Items[0].Name);
    }

    #endregion

    private async Task<EpisodeEntity> CreateEpisodeAsync(int showId, int patreonId, string title)
    {
        PatreonPostEntity post = new()
        {
            PatreonId = patreonId,
            Title = $"Post {patreonId}",
            Link = $"https://patreon.com/{patreonId}",
            Summary = $"Summary {patreonId}",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = $"https://audio.com/{patreonId}",
            ShowId = showId
        };

        EpisodeEntity episode = new()
        {
            Title = title,
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post,
            ShowId = showId
        };

        await InsertSingleInstanceOfEntityAsync(episode);

        return episode;
    }
}
