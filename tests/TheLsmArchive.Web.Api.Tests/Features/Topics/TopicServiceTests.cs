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
            ReadOnlyDbContext
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

    #region GetDetailsById

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetDetailsById_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetDetailsById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetDetailsById_WithNonExistentTopic_ReturnsNull()
    {
        // Arrange & Act
        TopicDetails? details = await _topicService.GetDetailsById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(details);
    }

    [Fact]
    public async Task GetDetailsById_WithExistingTopic_ReturnsFirstAndLastDiscussed()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        TopicEntity topic = new() { Name = "Test Topic", NormalizedName = "testtopic" };
        await InsertSingleInstanceOfEntityAsync(topic);

        PatreonPostEntity post1 = new()
        {
            PatreonId = 1, Title = "Post 1", Link = "https://patreon.com/1",
            Summary = "Summary 1", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/1", ShowId = show.Id
        };
        PatreonPostEntity post2 = new()
        {
            PatreonId = 2, Title = "Post 2", Link = "https://patreon.com/2",
            Summary = "Summary 2", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/2", ShowId = show.Id
        };

        EpisodeEntity ep1 = new()
        {
            Title = "First Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 2, 10, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity ep2 = new()
        {
            Title = "Latest Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 8, 25, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        TopicEpisodeEntity te1 = new() { TopicId = topic.Id, EpisodeId = ep1.Id };
        TopicEpisodeEntity te2 = new() { TopicId = topic.Id, EpisodeId = ep2.Id };
        await InsertSingleInstanceOfEntityAsync(te1);
        await InsertSingleInstanceOfEntityAsync(te2);

        // Act
        TopicDetails? details = await _topicService.GetDetailsById(topic.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(new DateOnly(2024, 2, 10), details.FirstDiscussed);
        Assert.Equal(new DateOnly(2024, 8, 25), details.LastDiscussed);
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
            PatreonId = 1, Title = "Post 1", Link = "https://patreon.com/1",
            Summary = "Summary 1", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/1", ShowId = show.Id
        };
        EpisodeEntity episode = new()
        {
            Title = "Episode 1", ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post, ShowId = show.Id
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
            PatreonId = 1, Title = "Post 1", Link = "https://patreon.com/1",
            Summary = "Summary 1", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/1", ShowId = show.Id
        };
        EpisodeEntity episode = new()
        {
            Title = "Episode 1", ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post, ShowId = show.Id
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

    #endregion

    #region GetByPersonId

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetByPersonId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _topicService.GetByPersonId(id, new PagedItemRequest(), TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByPersonId_WithNullPagedRequest_ThrowsArgumentNullException()
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _topicService.GetByPersonId(1, null!, TestContext.Current.CancellationToken));
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
            person.Id, new PagedItemRequest(), TestContext.Current.CancellationToken);

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
            person.Id, new PagedItemRequest(), TestContext.Current.CancellationToken);

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
            person.Id, new PagedItemRequest(SearchTerm: "PlayStation"), TestContext.Current.CancellationToken);

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
            person.Id, new PagedItemRequest(PageNumber: 2, PageSize: 1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Beta", result.Items[0].Name);
    }

    #endregion
}
