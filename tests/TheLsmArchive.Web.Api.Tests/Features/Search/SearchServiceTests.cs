using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Enums;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Search;

public sealed class SearchServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RunSearchAsync_WhenSearchingAllWithLongTerm_ReturnsPagedOrderedResultsIncludingEpisodes()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Search Test Show");

        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Retro Alpha", cancellationToken);
        TopicEntity topic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Retro Beta", cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Retro Gamma",
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
            8001,
            cancellationToken);

        SearchService searchService = CreateSut();
        SearchRequest request = new("retro", PageNumber: 2, PageSize: 1);

        // Act
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(request, cancellationToken);

        // Assert
        Equal(3, result.TotalCount);
        Equal(2, result.PageNumber);
        Equal(1, result.PageSize);
        Equal(3, result.TotalPages);
        Single(result.Items);
        Equal([new SearchResult(topic.Id, "Retro Beta", EntityType.Topic)], result.Items);
    }

    [Fact]
    public async Task RunSearchAsync_WhenSearchingAllWithShortTerm_ExcludesEpisodes()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Search Test Show");

        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Doom Guy", cancellationToken);
        TopicEntity topic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Doom Lore", cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Doom Eternal Review",
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero),
            8002,
            cancellationToken,
            summaryHtml: "<p>Doom coverage.</p>");

        SearchService searchService = CreateSut();
        SearchRequest request = new("doom");

        // Act
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(request, cancellationToken);

        // Assert
        Equal(2, result.TotalCount);
        Equal(
            [
                new SearchResult(person.Id, "Doom Guy", EntityType.Person),
                new SearchResult(topic.Id, "Doom Lore", EntityType.Topic)
            ],
            result.Items);
    }

    [Fact]
    public async Task RunSearchAsync_WhenSearchingPeople_ReturnsOnlyPeople()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Search Test Show");

        PersonEntity firstPerson = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Colin Moriarty", cancellationToken);
        PersonEntity secondPerson = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Colin's Friend", cancellationToken);
        await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Colin Was Here", cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "The Colin Episode",
            new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero),
            8003,
            cancellationToken);

        SearchService searchService = CreateSut();
        SearchRequest request = new("colin", EntityType.Person);

        // Act
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(request, cancellationToken);

        // Assert
        Equal(2, result.TotalCount);
        Equal(
            [
                new SearchResult(firstPerson.Id, "Colin Moriarty", EntityType.Person),
                new SearchResult(secondPerson.Id, "Colin's Friend", EntityType.Person)
            ],
            result.Items);
    }

    [Fact]
    public async Task RunSearchAsync_WhenSearchingTopicsWithNoMatches_ReturnsEmptyPagedResponse()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SearchService searchService = CreateSut();
        SearchRequest request = new("nonexistent", EntityType.Topic, PageNumber: 3, PageSize: 10);

        // Act
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(request, cancellationToken);

        // Assert
        Empty(result.Items);
        Equal(0, result.TotalCount);
        Equal(3, result.PageNumber);
        Equal(10, result.PageSize);
        Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task RunSearchAsync_WhenSearchingEpisodesWithShortTerm_ReturnsEpisodesMatchingTitleAndSummary()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Search Test Show");

        EpisodeEntity summaryMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Community Mailbag",
            new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero),
            8004,
            cancellationToken,
            summaryHtml: "<p>Doom takes over the conversation.</p>");

        EpisodeEntity titleMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Doom Retrospective",
            new DateTimeOffset(2026, 6, 5, 12, 0, 0, TimeSpan.Zero),
            8005,
            cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "PlayStation Talk",
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
            8006,
            cancellationToken,
            summaryHtml: "<p>No match here.</p>");

        SearchService searchService = CreateSut();
        SearchRequest request = new("doom", EntityType.Episode);

        // Act
        PagedResponse<SearchResult> result = await searchService.RunSearchAsync(request, cancellationToken);

        // Assert
        Equal(2, result.TotalCount);
        Equal(
            [
                new SearchResult(summaryMatch.Id, "Community Mailbag", EntityType.Episode),
                new SearchResult(titleMatch.Id, "Doom Retrospective", EntityType.Episode)
            ],
            result.Items);
    }

    [Fact]
    public async Task RunSearchAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        SearchService searchService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentNullException>(() =>
            searchService.RunSearchAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunSearchAsync_WhenEntityTypeIsUnsupported_ThrowsUnsupportedEntityTypeException()
    {
        // Arrange
        SearchService searchService = CreateSut();
        SearchRequest request = new("retro", (EntityType)999);

        // Act & Assert
        UnsupportedEntityTypeException exception = await ThrowsAsync<UnsupportedEntityTypeException>(() =>
            searchService.RunSearchAsync(request, TestContext.Current.CancellationToken));

        Equal("Unsupported entity type: 999", exception.Message);
    }

    private SearchService CreateSut() => new(Get<ILogger<SearchService>>(), Get<LsmArchiveDbContext>());
}
