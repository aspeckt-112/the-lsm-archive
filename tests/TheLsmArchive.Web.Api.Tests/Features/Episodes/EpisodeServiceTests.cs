using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class EpisodeServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly EpisodeService _episodeService;

    public EpisodeServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<EpisodeService>> loggerMock = new();

        _episodeService = new EpisodeService(
            loggerMock.Object,
            DbContext
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetById_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange & Act & Assert
#pragma warning disable IDE0022 // Use expression body for method
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _episodeService.GetById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022 // Use expression body for method
    }

    [Fact]
    public async Task GetById_WithValidIdButNonExistentEpisode_ReturnsNull()
    {
        // Arrange & Act
        Episode? episode = await _episodeService.GetById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(episode);
    }

    [Fact]
    public async Task GetRecent_WithRecentEpisodes_ReturnsCorrectEpisodes()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post1 = new() { PatreonId = 1, Title = "Post 1", Link = "https://patreon.com/1", Summary = "Summary 1", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/1", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 2, Title = "Post 2", Link = "https://patreon.com/2", Summary = "Summary 2", Published = DateTimeOffset.UtcNow.AddDays(-10), AudioUrl = "https://audio.com/2", ShowId = show.Id };

        EpisodeEntity recentEpisode = new()
        {
            Title = "Recent Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity oldEpisode = new()
        {
            Title = "Old Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-10),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(recentEpisode);
        await InsertSingleInstanceOfEntityAsync(oldEpisode);

        // Act
        List<Episode> recentEpisodes = await _episodeService.GetRecent(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(recentEpisodes);
        Assert.Equal("Recent Episode", recentEpisodes[0].Title);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WithEpisodes_ReturnsOneOfTheInsertedIds()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post1 = new() { PatreonId = 11, Title = "Post 11", Link = "https://patreon.com/11", Summary = "Summary 11", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/11", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 12, Title = "Post 12", Link = "https://patreon.com/12", Summary = "Summary 12", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/12", ShowId = show.Id };
        PatreonPostEntity post3 = new() { PatreonId = 13, Title = "Post 13", Link = "https://patreon.com/13", Summary = "Summary 13", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/13", ShowId = show.Id };

        EpisodeEntity episode1 = new() { Title = "Episode 1", ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1), PatreonPost = post1, ShowId = show.Id };
        EpisodeEntity episode2 = new() { Title = "Episode 2", ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-2), PatreonPost = post2, ShowId = show.Id };
        EpisodeEntity episode3 = new() { Title = "Episode 3", ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-3), PatreonPost = post3, ShowId = show.Id };

        await InsertSingleInstanceOfEntityAsync(episode1);
        await InsertSingleInstanceOfEntityAsync(episode2);
        await InsertSingleInstanceOfEntityAsync(episode3);

        int[] insertedIds = [episode1.Id, episode2.Id, episode3.Id];

        // Act
        int randomEpisodeId = await _episodeService.GetRandomEpisodeId(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(randomEpisodeId, insertedIds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetMostRecentByPersonId_WithInvalidId_ThrowsArgumentOutOfRangeException(int id)
    {
#pragma warning disable IDE0022 // Use expression body for method
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _episodeService.GetMostRecentByPersonId(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022 // Use expression body for method
    }

    [Fact]
    public async Task GetMostRecentByPersonId_WithValidIdButNoPerson_ReturnsNull()
    {
        // Arrange & Act
        Episode? episode = await _episodeService.GetMostRecentByPersonId(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(episode);
    }

    [Fact]
    public async Task GetMostRecentByPersonId_WithMultipleEpisodes_ReturnsMostRecent()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "test person" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new() { PatreonId = 101, Title = "Post 101", Link = "https://patreon.com/101", Summary = "Summary 101", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/101", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 102, Title = "Post 102", Link = "https://patreon.com/102", Summary = "Summary 102", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/102", ShowId = show.Id };

        EpisodeEntity olderEpisode = new()
        {
            Title = "Older Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-30),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity newerEpisode = new()
        {
            Title = "Newer Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(olderEpisode);
        await InsertSingleInstanceOfEntityAsync(newerEpisode);

        PersonEpisodeEntity pe1 = new() { PersonId = person.Id, EpisodeId = olderEpisode.Id };
        PersonEpisodeEntity pe2 = new() { PersonId = person.Id, EpisodeId = newerEpisode.Id };

        await InsertSingleInstanceOfEntityAsync(pe1);
        await InsertSingleInstanceOfEntityAsync(pe2);

        // Act
        Episode? result = await _episodeService.GetMostRecentByPersonId(person.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Newer Episode", result.Title);
        Assert.Equal(newerEpisode.Id, result.Id);
    }

    #region GetById — additional

    [Fact]
    public async Task GetById_WithExistingEpisode_ReturnsEpisode()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post = new()
        {
            PatreonId = 200,
            Title = "Post 200",
            Link = "https://patreon.com/200",
            Summary = "<p>A summary</p>",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/200",
            ShowId = show.Id
        };
        EpisodeEntity episodeEntity = new()
        {
            Title = "My Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 5, 10, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post,
            ShowId = show.Id
        };
        await InsertSingleInstanceOfEntityAsync(episodeEntity);

        // Act
        Episode? episode = await _episodeService.GetById(episodeEntity.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(episode);
        Assert.Equal(episodeEntity.Id, episode.Id);
        Assert.Equal("My Episode", episode.Title);
        Assert.Equal(new DateOnly(2024, 5, 10), episode.ReleaseDate);
        Assert.Equal("https://patreon.com/200", episode.PatreonPostLink);
        Assert.Equal("<p>A summary</p>", episode.SummaryHtml);
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
            _episodeService.GetByPersonId(id, new PagedItemRequest(), true, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByPersonId_WithNullPagedRequest_ThrowsArgumentNullException()
    {
#pragma warning disable IDE0022
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _episodeService.GetByPersonId(1, null!, true, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByPersonId_WithNoAssociatedEpisodes_ReturnsEmptyPagedResponse()
    {
        // Arrange
        PersonEntity person = new() { Name = "Lonely Person", NormalizedName = "lonelyperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        // Act
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetByPersonId_WithAssociatedEpisodes_ReturnsPagedEntries()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new()
        {
            PatreonId = 301,
            Title = "Post 301",
            Link = "https://patreon.com/301",
            Summary = "Summary 301",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/301",
            ShowId = show.Id
        };
        PatreonPostEntity post2 = new()
        {
            PatreonId = 302,
            Title = "Post 302",
            Link = "https://patreon.com/302",
            Summary = "Summary 302",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/302",
            ShowId = show.Id
        };

        EpisodeEntity ep1 = new()
        {
            Title = "Alpha Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-2),
            PatreonPost = post1,
            ShowId = show.Id
        };
        EpisodeEntity ep2 = new()
        {
            Title = "Beta Episode",
            ReleaseDateUtc = DateTimeOffset.UtcNow.AddDays(-1),
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        PersonEpisodeEntity pe1 = new() { PersonId = person.Id, EpisodeId = ep1.Id };
        PersonEpisodeEntity pe2 = new() { PersonId = person.Id, EpisodeId = ep2.Id };
        await InsertSingleInstanceOfEntityAsync(pe1);
        await InsertSingleInstanceOfEntityAsync(pe2);

        // Act
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetByPersonId_WithSearchTerm_FiltersResults()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new()
        {
            PatreonId = 401,
            Title = "Post 401",
            Link = "https://patreon.com/401",
            Summary = "Summary about gaming",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/401",
            ShowId = show.Id
        };
        PatreonPostEntity post2 = new()
        {
            PatreonId = 402,
            Title = "Post 402",
            Link = "https://patreon.com/402",
            Summary = "Summary about cooking",
            Published = DateTimeOffset.UtcNow,
            AudioUrl = "https://audio.com/402",
            ShowId = show.Id
        };

        EpisodeEntity ep1 = new()
        {
            Title = "Gaming Discussion",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post1,
            ShowId = show.Id
        };
        EpisodeEntity ep2 = new()
        {
            Title = "Cooking Tips",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPost = post2,
            ShowId = show.Id
        };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        PersonEpisodeEntity pe1 = new() { PersonId = person.Id, EpisodeId = ep1.Id };
        PersonEpisodeEntity pe2 = new() { PersonId = person.Id, EpisodeId = ep2.Id };
        await InsertSingleInstanceOfEntityAsync(pe1);
        await InsertSingleInstanceOfEntityAsync(pe2);

        // Act — search by title
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(SearchTerm: "Gaming"), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Gaming Discussion", result.Items[0].Title);
    }

    [Fact]
    public async Task GetByPersonId_WithPaging_ReturnsCorrectPage()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new() { PatreonId = 501, Title = "Post 501", Link = "https://patreon.com/501", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/501", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 502, Title = "Post 502", Link = "https://patreon.com/502", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/502", ShowId = show.Id };
        PatreonPostEntity post3 = new() { PatreonId = 503, Title = "Post 503", Link = "https://patreon.com/503", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/503", ShowId = show.Id };

        EpisodeEntity ep1 = new() { Title = "Alpha", ReleaseDateUtc = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), PatreonPost = post1, ShowId = show.Id };
        EpisodeEntity ep2 = new() { Title = "Beta", ReleaseDateUtc = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero), PatreonPost = post2, ShowId = show.Id };
        EpisodeEntity ep3 = new() { Title = "Gamma", ReleaseDateUtc = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero), PatreonPost = post3, ShowId = show.Id };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);
        await InsertSingleInstanceOfEntityAsync(ep3);

        PersonEpisodeEntity pe1 = new() { PersonId = person.Id, EpisodeId = ep1.Id };
        PersonEpisodeEntity pe2 = new() { PersonId = person.Id, EpisodeId = ep2.Id };
        PersonEpisodeEntity pe3 = new() { PersonId = person.Id, EpisodeId = ep3.Id };
        await InsertSingleInstanceOfEntityAsync(pe1);
        await InsertSingleInstanceOfEntityAsync(pe2);
        await InsertSingleInstanceOfEntityAsync(pe3);

        // Act — page 2, size 1, descending (ordered by date desc: Gamma, Beta, Alpha)
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(PageNumber: 2, PageSize: 1), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Beta", result.Items[0].Title);
    }

    [Fact]
    public async Task GetByPersonId_SortDescending_ReturnsNewestFirst()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new() { PatreonId = 601, Title = "Post 601", Link = "https://patreon.com/601", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/601", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 602, Title = "Post 602", Link = "https://patreon.com/602", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/602", ShowId = show.Id };

        EpisodeEntity older = new() { Title = "Older Episode", ReleaseDateUtc = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero), PatreonPost = post1, ShowId = show.Id };
        EpisodeEntity newer = new() { Title = "Newer Episode", ReleaseDateUtc = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero), PatreonPost = post2, ShowId = show.Id };

        await InsertSingleInstanceOfEntityAsync(older);
        await InsertSingleInstanceOfEntityAsync(newer);

        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = older.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = newer.Id });

        // Act
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Newer Episode", result.Items[0].Title);
        Assert.Equal("Older Episode", result.Items[1].Title);
    }

    [Fact]
    public async Task GetByPersonId_SortAscending_ReturnsOldestFirst()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        PatreonPostEntity post1 = new() { PatreonId = 701, Title = "Post 701", Link = "https://patreon.com/701", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/701", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 702, Title = "Post 702", Link = "https://patreon.com/702", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/702", ShowId = show.Id };

        EpisodeEntity older = new() { Title = "Older Episode", ReleaseDateUtc = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero), PatreonPost = post1, ShowId = show.Id };
        EpisodeEntity newer = new() { Title = "Newer Episode", ReleaseDateUtc = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero), PatreonPost = post2, ShowId = show.Id };

        await InsertSingleInstanceOfEntityAsync(older);
        await InsertSingleInstanceOfEntityAsync(newer);

        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = older.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = newer.Id });

        // Act
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(), false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Older Episode", result.Items[0].Title);
        Assert.Equal("Newer Episode", result.Items[1].Title);
    }

    [Fact]
    public async Task GetByPersonId_IncludesTopicsInEntries()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity topic1 = new() { Name = "Topic Alpha", NormalizedName = "topicalpha" };
        TopicEntity topic2 = new() { Name = "Topic Beta", NormalizedName = "topicbeta" };
        await InsertSingleInstanceOfEntityAsync(topic1);
        await InsertSingleInstanceOfEntityAsync(topic2);

        PatreonPostEntity post = new() { PatreonId = 801, Title = "Post 801", Link = "https://patreon.com/801", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/801", ShowId = show.Id };

        EpisodeEntity episode = new() { Title = "Episode With Topics", ReleaseDateUtc = DateTimeOffset.UtcNow, PatreonPost = post, ShowId = show.Id };
        await InsertSingleInstanceOfEntityAsync(episode);

        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = episode.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topic1.Id, EpisodeId = episode.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = topic2.Id, EpisodeId = episode.Id });

        // Act
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Topics.Count);
        Assert.Contains(result.Items[0].Topics, t => t.Name == "Topic Alpha");
        Assert.Contains(result.Items[0].Topics, t => t.Name == "Topic Beta");
    }

    [Fact]
    public async Task GetByPersonId_WithSearchTermMatchingTopicName_FiltersResults()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

        TopicEntity gamingTopic = new() { Name = "Gaming", NormalizedName = "gaming" };
        TopicEntity cookingTopic = new() { Name = "Cooking", NormalizedName = "cooking" };
        await InsertSingleInstanceOfEntityAsync(gamingTopic);
        await InsertSingleInstanceOfEntityAsync(cookingTopic);

        PatreonPostEntity post1 = new() { PatreonId = 901, Title = "Post 901", Link = "https://patreon.com/901", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/901", ShowId = show.Id };
        PatreonPostEntity post2 = new() { PatreonId = 902, Title = "Post 902", Link = "https://patreon.com/902", Summary = "S", Published = DateTimeOffset.UtcNow, AudioUrl = "https://audio.com/902", ShowId = show.Id };

        EpisodeEntity ep1 = new() { Title = "Episode One", ReleaseDateUtc = DateTimeOffset.UtcNow, PatreonPost = post1, ShowId = show.Id };
        EpisodeEntity ep2 = new() { Title = "Episode Two", ReleaseDateUtc = DateTimeOffset.UtcNow, PatreonPost = post2, ShowId = show.Id };

        await InsertSingleInstanceOfEntityAsync(ep1);
        await InsertSingleInstanceOfEntityAsync(ep2);

        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = ep1.Id });
        await InsertSingleInstanceOfEntityAsync(new PersonEpisodeEntity { PersonId = person.Id, EpisodeId = ep2.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = gamingTopic.Id, EpisodeId = ep1.Id });
        await InsertSingleInstanceOfEntityAsync(new TopicEpisodeEntity { TopicId = cookingTopic.Id, EpisodeId = ep2.Id });

        // Act — search by topic name
        PagedResponse<PersonTimelineEntry> result = await _episodeService.GetByPersonId(
            person.Id, new PagedItemRequest(SearchTerm: "Gaming"), true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Episode One", result.Items[0].Title);
    }

    #endregion
}
