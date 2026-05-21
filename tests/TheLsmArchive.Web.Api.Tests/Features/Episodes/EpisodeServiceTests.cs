using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Request;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Episodes;

public sealed class EpisodeServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetById_WhenEpisodeExists_ReturnsProjectedEpisode()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        DateTimeOffset releaseDateUtc = new(2026, 5, 10, 16, 45, 0, TimeSpan.Zero);
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Sacred Symbols 400",
            releaseDateUtc,
            4100,
            cancellationToken,
            summaryHtml: "<p>Big anniversary episode.</p>",
            patreonPostLink: "https://www.patreon.com/posts/4100");

        EpisodeService episodeService = CreateSut();

        // Act
        Episode? result = await episodeService.GetById(episode.Id, cancellationToken);

        // Assert
        NotNull(result);
        Equal(episode.Id, result.Id);
        Equal("Sacred Symbols 400", result.Title);
        Equal(DateOnly.FromDateTime(releaseDateUtc.DateTime), result.ReleaseDate);
        Equal("https://www.patreon.com/posts/4100", result.PatreonPostLink);
        Equal("<p>Big anniversary episode.</p>", result.SummaryHtml);
    }

    [Fact]
    public async Task GetById_WhenEpisodeDoesNotExist_ReturnsNull()
    {
        // Arrange
        EpisodeService episodeService = CreateSut();

        // Act
        Episode? result = await episodeService.GetById(999_999, TestContext.Current.CancellationToken);

        // Assert
        Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        EpisodeService episodeService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => episodeService.GetById(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByPersonId_WhenEpisodesExist_ReturnsPagedTimelineWithTopics()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Chris Ray Gun", cancellationToken);
        TopicEntity featuredTopic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Politics", cancellationToken);

        EpisodeEntity firstEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode One",
            new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero),
            5001,
            cancellationToken);

        EpisodeEntity secondEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode Two",
            new DateTimeOffset(2026, 2, 5, 12, 0, 0, TimeSpan.Zero),
            5002,
            cancellationToken);

        EpisodeEntity thirdEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Episode Three",
            new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero),
            5003,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, firstEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, secondEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, thirdEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, featuredTopic, secondEpisode, cancellationToken);

        EpisodeService episodeService = CreateSut();
        PagedItemRequest request = new(PageNumber: 2, PageSize: 1);

        // Act
        PagedResponse<PersonTimelineEntry> result = await episodeService.GetByPersonId(
            person.Id,
            request,
            sortDescending: false,
            cancellationToken);

        // Assert
        Equal(3, result.TotalCount);
        Equal(2, result.PageNumber);
        Equal(1, result.PageSize);
        Equal(3, result.TotalPages);
        Single(result.Items);

        PersonTimelineEntry entry = result.Items[0];
        Equal(secondEpisode.Id, entry.EpisodeId);
        Equal("Episode Two", entry.Title);
        Equal(DateOnly.FromDateTime(secondEpisode.ReleaseDateUtc.DateTime), entry.ReleaseDate);
        Equal("https://www.patreon.com/posts/5002", entry.PatreonPostLink);
        Equal([new Topic(featuredTopic.Id, "Politics")], entry.Topics);
    }

    [Fact]
    public async Task GetByPersonId_WhenSearchTermProvided_FiltersByTitleAndTopicAndSortsDescending()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Dustin Furman", cancellationToken);
        TopicEntity nintendoTopic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Nintendo", cancellationToken);
        TopicEntity generalTopic = await EpisodeTestDataHelper.CreateTopicAsync(dbContext, "Community", cancellationToken);

        EpisodeEntity titleMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Nintendo Direct Reactions",
            new DateTimeOffset(2026, 2, 10, 14, 0, 0, TimeSpan.Zero),
            5101,
            cancellationToken);

        EpisodeEntity topicMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Mailbag Special",
            new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero),
            5102,
            cancellationToken);

        EpisodeEntity nonMatch = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "PlayStation Showcase",
            new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero),
            5103,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, titleMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, topicMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, nonMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, generalTopic, titleMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, nintendoTopic, topicMatch, cancellationToken);
        await EpisodeTestDataHelper.LinkTopicToEpisodeAsync(dbContext, generalTopic, nonMatch, cancellationToken);

        EpisodeService episodeService = CreateSut();
        PagedItemRequest request = new(SearchTerm: "nintendo");

        // Act
        PagedResponse<PersonTimelineEntry> result = await episodeService.GetByPersonId(
            person.Id,
            request,
            sortDescending: true,
            cancellationToken);

        // Assert
        Equal(2, result.TotalCount);
        Equal(2, result.Items.Count);
        Equal([topicMatch.Id, titleMatch.Id], result.Items.Select(item => item.EpisodeId).ToList());
        Equal(["Mailbag Special", "Nintendo Direct Reactions"], result.Items.Select(item => item.Title).ToList());
    }

    [Fact]
    public async Task GetByPersonId_WhenPersonHasNoEpisodes_ReturnsEmptyPagedResponse()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Ben Smith", cancellationToken);
        EpisodeService episodeService = CreateSut();

        // Act
        PagedResponse<PersonTimelineEntry> result = await episodeService.GetByPersonId(
            person.Id,
            new PagedItemRequest(),
            sortDescending: true,
            cancellationToken);

        // Assert
        Empty(result.Items);
        Equal(0, result.TotalCount);
        Equal(0, result.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPersonId_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        EpisodeService episodeService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => episodeService.GetByPersonId(
            id,
            new PagedItemRequest(),
            sortDescending: true,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByPersonId_WhenPagedRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        EpisodeService episodeService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentNullException>(() => episodeService.GetByPersonId(
            1,
            null!,
            sortDescending: true,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMostRecentByPersonId_WhenEpisodesExist_ReturnsMostRecentEpisode()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Matty", cancellationToken);

        EpisodeEntity olderEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Older Appearance",
            new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            5201,
            cancellationToken);

        EpisodeEntity newerEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Latest Appearance",
            new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero),
            5202,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, olderEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, newerEpisode, cancellationToken);

        EpisodeService episodeService = CreateSut();

        // Act
        Episode? result = await episodeService.GetMostRecentByPersonId(person.Id, cancellationToken);

        // Assert
        NotNull(result);
        Equal(newerEpisode.Id, result.Id);
        Equal("Latest Appearance", result.Title);
    }

    [Fact]
    public async Task GetMostRecentByPersonId_WhenPersonHasNoEpisodes_ReturnsNull()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Stelly", cancellationToken);
        EpisodeService episodeService = CreateSut();

        // Act
        Episode? result = await episodeService.GetMostRecentByPersonId(person.Id, cancellationToken);

        // Assert
        Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetMostRecentByPersonId_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        EpisodeService episodeService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => episodeService.GetMostRecentByPersonId(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRecent_WhenRecentEpisodesExist_ReturnsOnlyEpisodesFromLastSevenDaysInDescendingOrder()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        EpisodeEntity oldestIncluded = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Week-Old Episode",
            now.AddDays(-6),
            5301,
            cancellationToken);

        EpisodeEntity newestIncluded = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Fresh Episode",
            now.AddDays(-1),
            5302,
            cancellationToken);

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Archive Episode",
            now.AddDays(-12),
            5303,
            cancellationToken);

        EpisodeService episodeService = CreateSut();

        // Act
        List<Episode> result = await episodeService.GetRecent(cancellationToken);

        // Assert
        Equal(2, result.Count);
        Equal([newestIncluded.Id, oldestIncluded.Id], result.Select(episode => episode.Id).ToList());
    }

    [Fact]
    public async Task GetRecent_WhenNoRecentEpisodesExist_ReturnsEmptyList()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Old Episode",
            now.AddDays(-20),
            5401,
            cancellationToken);

        EpisodeService episodeService = CreateSut();

        // Act
        List<Episode> result = await episodeService.GetRecent(cancellationToken);

        // Assert
        Empty(result);
    }

    [Fact]
    public async Task GetRandomEpisodeId_WhenOnlyOneEpisodeExists_ReturnsThatEpisodeId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Episode Test Show");
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Only Episode",
            new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            5501,
            cancellationToken);

        EpisodeService episodeService = CreateSut();

        // Act
        int result = await episodeService.GetRandomEpisodeId(cancellationToken);

        // Assert
        Equal(episode.Id, result);
    }

    private EpisodeService CreateSut() => new(Get<ILogger<EpisodeService>>(), Get<LsmArchiveDbContext>());
}

