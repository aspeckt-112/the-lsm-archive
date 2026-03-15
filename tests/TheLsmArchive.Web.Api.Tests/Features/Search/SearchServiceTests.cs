using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Search;

namespace TheLsmArchive.Web.Api.Tests.Features.Search;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class SearchServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly SearchService _searchService;

    public SearchServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<SearchService>> loggerMock = new();

        _searchService = new SearchService(
            loggerMock.Object,
            ReadOnlyDbContext
        );
    }

    [Fact]
    public async Task RunSearchAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _searchService.RunSearchAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunSearchAsync_WithUnsupportedEntityType_ThrowsUnsupportedEntityTypeException()
    {
        // Arrange
        SearchRequest request = new("test", (EntityType)99);

        // Act & Assert
        await Assert.ThrowsAsync<UnsupportedEntityTypeException>(() =>
            _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunSearchAsync_WithNoMatchingPerson_ReturnsEmptyResult()
    {
        // Arrange
        SearchRequest request = new("nomatch_xj9qz7_abc", EntityType.Person);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task RunSearchAsync_WithMatchingPersonName_ReturnsPersonResult()
    {
        // Arrange
        PersonEntity person = new() { Name = "ST_Person_Alice", NormalizedName = "st_person_alice" };
        await InsertSingleInstanceOfEntityAsync(person);

        SearchRequest request = new("ST_Person_Alice", EntityType.Person);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        SearchResult item = Assert.Single(result.Items);
        Assert.Equal("ST_Person_Alice", item.Matched);
        Assert.Equal(EntityType.Person, item.EntityType);
    }

    [Fact]
    public async Task RunSearchAsync_WithMatchingTopicName_ReturnsTopicResult()
    {
        // Arrange
        TopicEntity topic = new() { Name = "ST_Topic_Philosophy", NormalizedName = "st_topic_philosophy" };
        await InsertSingleInstanceOfEntityAsync(topic);

        SearchRequest request = new("ST_Topic_Philosophy", EntityType.Topic);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        SearchResult item = Assert.Single(result.Items);
        Assert.Equal("ST_Topic_Philosophy", item.Matched);
        Assert.Equal(EntityType.Topic, item.EntityType);
    }

    [Fact]
    public async Task RunSearchAsync_WithMatchingEpisodeTitle_ReturnsEpisodeResult()
    {
        // Arrange
        ShowEntity show = new() { Name = "ST_Episode_Show" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post = new()
        {
            ShowId = show.Id,
            PatreonId = 9001,
            Title = "ST_Episode_Quantum",
            Published = DateTimeOffset.UtcNow,
            Summary = "A test summary.",
            Link = "https://example.com/st-ep-post",
            AudioUrl = "https://example.com/st-ep-audio.mp3"
        };

        await InsertSingleInstanceOfEntityAsync(post);

        EpisodeEntity episode = new()
        {
            ShowId = show.Id,
            Title = "ST_Episode_Quantum",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPostId = post.Id
        };

        await InsertSingleInstanceOfEntityAsync(episode);

        SearchRequest request = new("ST_Episode_Quantum", EntityType.Episode);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        SearchResult item = Assert.Single(result.Items);
        Assert.Equal("ST_Episode_Quantum", item.Matched);
        Assert.Equal(EntityType.Episode, item.EntityType);
    }

    [Fact]
    public async Task RunSearchAsync_WithAllTypeAndShortTerm_ExcludesEpisodes()
    {
        // Arrange
        // "ab9z" is 4 characters — below the 5-character threshold for including episodes in All searches
        const string shortTerm = "ab9z";

        PersonEntity person = new() { Name = "ab9z_person_uniq", NormalizedName = "ab9z_person_uniq" };
        TopicEntity topic = new() { Name = "ab9z_topic_uniq", NormalizedName = "ab9z_topic_uniq" };
        await InsertSingleInstanceOfEntityAsync(person);
        await InsertSingleInstanceOfEntityAsync(topic);

        ShowEntity show = new() { Name = "ab9z Show" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post = new()
        {
            ShowId = show.Id,
            PatreonId = 9002,
            Title = "ab9z episode title",
            Published = DateTimeOffset.UtcNow,
            Summary = "ab9z summary",
            Link = "https://example.com/ab9z-post",
            AudioUrl = "https://example.com/ab9z-audio.mp3"
        };

        await InsertSingleInstanceOfEntityAsync(post);

        EpisodeEntity episode = new()
        {
            ShowId = show.Id,
            Title = "ab9z episode title",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPostId = post.Id
        };

        await InsertSingleInstanceOfEntityAsync(episode);

        SearchRequest request = new(shortTerm, EntityType.All);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(result.Items, r => r.EntityType == EntityType.Episode);
        Assert.Contains(result.Items, r => r.EntityType == EntityType.Person);
        Assert.Contains(result.Items, r => r.EntityType == EntityType.Topic);
    }

    [Fact]
    public async Task RunSearchAsync_WithAllTypeAndLongTerm_IncludesEpisodes()
    {
        // Arrange
        // "srchalluniq" is 11 characters — above the 5-character threshold for including episodes in All searches
        const string longTerm = "srchalluniq";

        PersonEntity person = new() { Name = "srchalluniq_person", NormalizedName = "srchalluniq_person" };
        TopicEntity topic = new() { Name = "srchalluniq_topic", NormalizedName = "srchalluniq_topic" };
        await InsertSingleInstanceOfEntityAsync(person);
        await InsertSingleInstanceOfEntityAsync(topic);

        ShowEntity show = new() { Name = "srchalluniq Show" };
        await InsertSingleInstanceOfEntityAsync(show);

        PatreonPostEntity post = new()
        {
            ShowId = show.Id,
            PatreonId = 9003,
            Title = "srchalluniq episode title",
            Published = DateTimeOffset.UtcNow,
            Summary = "srchalluniq summary",
            Link = "https://example.com/srchalluniq-post",
            AudioUrl = "https://example.com/srchalluniq-audio.mp3"
        };

        await InsertSingleInstanceOfEntityAsync(post);

        EpisodeEntity episode = new()
        {
            ShowId = show.Id,
            Title = "srchalluniq episode title",
            ReleaseDateUtc = DateTimeOffset.UtcNow,
            PatreonPostId = post.Id
        };

        await InsertSingleInstanceOfEntityAsync(episode);

        SearchRequest request = new(longTerm, EntityType.All);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(result.Items, r => r.EntityType == EntityType.Episode);
        Assert.Contains(result.Items, r => r.EntityType == EntityType.Person);
        Assert.Contains(result.Items, r => r.EntityType == EntityType.Topic);
    }

    [Fact]
    public async Task RunSearchAsync_WithMultiplePersonResults_ReturnsOrderedByName()
    {
        // Arrange
        PersonEntity charlie = new() { Name = "ordtest_Charlie", NormalizedName = "ordtest_charlie" };
        PersonEntity alpha = new() { Name = "ordtest_Alpha", NormalizedName = "ordtest_alpha" };
        PersonEntity bravo = new() { Name = "ordtest_Bravo", NormalizedName = "ordtest_bravo" };
        await InsertSingleInstanceOfEntityAsync(charlie);
        await InsertSingleInstanceOfEntityAsync(alpha);
        await InsertSingleInstanceOfEntityAsync(bravo);

        SearchRequest request = new("ordtest_", EntityType.Person);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        List<SearchResult> items = result.Items;
        Assert.Equal("ordtest_Alpha", items[0].Matched);
        Assert.Equal("ordtest_Bravo", items[1].Matched);
        Assert.Equal("ordtest_Charlie", items[2].Matched);
    }

    [Fact]
    public async Task RunSearchAsync_WithPaging_ReturnsCorrectPage()
    {
        // Arrange
        PersonEntity first = new() { Name = "pgtest_A", NormalizedName = "pgtest_a" };
        PersonEntity second = new() { Name = "pgtest_B", NormalizedName = "pgtest_b" };
        PersonEntity third = new() { Name = "pgtest_C", NormalizedName = "pgtest_c" };
        await InsertSingleInstanceOfEntityAsync(first);
        await InsertSingleInstanceOfEntityAsync(second);
        await InsertSingleInstanceOfEntityAsync(third);

        // Page 2, size 1 — should return the second alphabetical result
        SearchRequest request = new("pgtest_", EntityType.Person, PageNumber: 2, PageSize: 1);

        // Act
        PagedResponse<SearchResult> result = await _searchService.RunSearchAsync(request, TestContext.Current.CancellationToken);

        // Assert
        SearchResult item = Assert.Single(result.Items);
        Assert.Equal("pgtest_B", item.Matched);
        Assert.Equal(3, result.TotalCount);
    }
}
