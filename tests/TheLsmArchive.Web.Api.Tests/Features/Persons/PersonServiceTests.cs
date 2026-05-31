using Microsoft.Extensions.Logging;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Tests.TestSupport.Helpers;

namespace TheLsmArchive.Web.Api.Tests.Features.Persons;

public sealed class PersonServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetById_WhenPersonExists_ReturnsProjectedPerson()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Colin Moriarty", cancellationToken);
        PersonService personService = CreateSut();

        // Act
        Person? result = await personService.GetById(person.Id, cancellationToken);

        // Assert
        Equal(new Person(person.Id, "Colin Moriarty"), result);
    }

    [Fact]
    public async Task GetById_WhenPersonDoesNotExist_ReturnsNull()
    {
        // Arrange
        PersonService personService = CreateSut();

        // Act
        Person? result = await personService.GetById(999_999, TestContext.Current.CancellationToken);

        // Assert
        Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        PersonService personService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => personService.GetById(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetDetailsById_WhenPersonHasEpisodes_ReturnsFirstAndLastAppearanceDates()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        PersonEntity person = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Matty", cancellationToken);

        EpisodeEntity earliestEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Early Appearance",
            new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero),
            7001,
            cancellationToken);

        EpisodeEntity middleEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Middle Appearance",
            new DateTimeOffset(2026, 3, 12, 12, 0, 0, TimeSpan.Zero),
            7002,
            cancellationToken);

        EpisodeEntity latestEpisode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Latest Appearance",
            new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero),
            7003,
            cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, middleEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, latestEpisode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, person, earliestEpisode, cancellationToken);

        PersonService personService = CreateSut();

        // Act
        PersonDetails? result = await personService.GetDetailsById(person.Id, cancellationToken);

        // Assert
        Equal(
            new PersonDetails(
                FirstAppeared: new DateOnly(2026, 1, 12),
                LastAppeared: new DateOnly(2026, 5, 12)),
            result);
    }

    [Fact]
    public async Task GetDetailsById_WhenPersonDoesNotExist_ReturnsNull()
    {
        // Arrange
        PersonService personService = CreateSut();

        // Act
        PersonDetails? result = await personService.GetDetailsById(999_999, TestContext.Current.CancellationToken);

        // Assert
        Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDetailsById_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        PersonService personService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => personService.GetDetailsById(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByEpisodeId_WhenPeopleExist_ReturnsAlphabeticallySortedPeople()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "Sacred Symbols 350",
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            7101,
            cancellationToken);

        PersonEntity thirdAlphabetically = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Matty", cancellationToken);
        PersonEntity firstAlphabetically = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Ben Smith", cancellationToken);
        PersonEntity secondAlphabetically = await EpisodeTestDataHelper.CreatePersonAsync(dbContext, "Chris Ray Gun", cancellationToken);

        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, thirdAlphabetically, episode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, firstAlphabetically, episode, cancellationToken);
        await EpisodeTestDataHelper.LinkPersonToEpisodeAsync(dbContext, secondAlphabetically, episode, cancellationToken);

        PersonService personService = CreateSut();

        // Act
        List<Person> result = await personService.GetByEpisodeId(episode.Id, cancellationToken);

        // Assert
        Equal(
            [
                new Person(firstAlphabetically.Id, "Ben Smith"),
                new Person(secondAlphabetically.Id, "Chris Ray Gun"),
                new Person(thirdAlphabetically.Id, "Matty")
            ],
            result);
    }

    [Fact]
    public async Task GetByEpisodeId_WhenEpisodeHasNoPeople_ReturnsEmptyList()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        ShowEntity show = await ShowTestDataHelper.CreateShowAsync(dbContext, cancellationToken, "Person Test Show");
        EpisodeEntity episode = await EpisodeTestDataHelper.CreateEpisodeAsync(
            dbContext,
            show,
            "No Guests",
            new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero),
            7102,
            cancellationToken);

        PersonService personService = CreateSut();

        // Act
        List<Person> result = await personService.GetByEpisodeId(episode.Id, cancellationToken);

        // Assert
        Empty(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByEpisodeId_WhenIdIsInvalid_ThrowsArgumentOutOfRangeException(int id)
    {
        // Arrange
        PersonService personService = CreateSut();

        // Act & Assert
        await ThrowsAsync<ArgumentOutOfRangeException>(() => personService.GetByEpisodeId(id, TestContext.Current.CancellationToken));
    }

    private PersonService CreateSut() => new(Get<ILogger<PersonService>>(), Get<LsmArchiveDbContext>());
}

