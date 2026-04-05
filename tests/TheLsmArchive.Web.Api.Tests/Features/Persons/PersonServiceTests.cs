using Microsoft.Extensions.Logging;

using Moq;

using TheLsmArchive.Database.Entities;
using TheLsmArchive.Models.Response;
using TheLsmArchive.Web.Api.Features.Persons;

namespace TheLsmArchive.Web.Api.Tests.Features.Persons;

[Collection(nameof(ServiceIntegrationTestFixture))]
public class PersonServiceTests : BaseServiceIntegrationTest, IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly PersonService _personService;

    public PersonServiceTests(ServiceIntegrationTestFixture fixture) : base(fixture)
    {
        Mock<ILogger<PersonService>> loggerMock = new();

        _personService = new PersonService(
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
            _personService.GetById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetById_WithValidIdButNonExistentPerson_ReturnsNull()
    {
        // Arrange & Act
        Person? person = await _personService.GetById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(person);
    }

    [Fact]
    public async Task GetById_WithExistingPerson_ReturnsPerson()
    {
        // Arrange
        PersonEntity personEntity = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(personEntity);

        // Act
        Person? person = await _personService.GetById(personEntity.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(person);
        Assert.Equal(personEntity.Id, person.Id);
        Assert.Equal("Test Person", person.Name);
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
            _personService.GetDetailsById(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetDetailsById_WithNonExistentPerson_ReturnsNull()
    {
        // Arrange & Act
        PersonDetails? details = await _personService.GetDetailsById(9999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(details);
    }

    [Fact]
    public async Task GetDetailsById_WithExistingPerson_ReturnsFirstAndLastAppeared()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person = new() { Name = "Test Person", NormalizedName = "testperson" };
        await InsertSingleInstanceOfEntityAsync(person);

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
            ReleaseDateUtc = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
            PatreonPost = post1,
            ShowId = show.Id
        };

        EpisodeEntity ep2 = new()
        {
            Title = "Latest Episode",
            ReleaseDateUtc = new DateTimeOffset(2024, 6, 20, 0, 0, 0, TimeSpan.Zero),
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
        PersonDetails? details = await _personService.GetDetailsById(person.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(details);
        Assert.Equal(new DateOnly(2024, 1, 15), details.FirstAppeared);
        Assert.Equal(new DateOnly(2024, 6, 20), details.LastAppeared);
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
            _personService.GetByEpisodeId(id, TestContext.Current.CancellationToken));
#pragma warning restore IDE0022
    }

    [Fact]
    public async Task GetByEpisodeId_WithNoAssociatedPersons_ReturnsEmptyList()
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
        List<Person> people = await _personService.GetByEpisodeId(episode.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(people);
    }

    [Fact]
    public async Task GetByEpisodeId_WithAssociatedPersons_ReturnsPersonList()
    {
        // Arrange
        ShowEntity show = new() { Name = "Show 1" };
        await InsertSingleInstanceOfEntityAsync(show);

        PersonEntity person1 = new() { Name = "Person A", NormalizedName = "persona" };
        PersonEntity person2 = new() { Name = "Person B", NormalizedName = "personb" };
        await InsertSingleInstanceOfEntityAsync(person1);
        await InsertSingleInstanceOfEntityAsync(person2);

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

        PersonEpisodeEntity pe1 = new() { PersonId = person1.Id, EpisodeId = episode.Id };
        PersonEpisodeEntity pe2 = new() { PersonId = person2.Id, EpisodeId = episode.Id };
        await InsertSingleInstanceOfEntityAsync(pe1);
        await InsertSingleInstanceOfEntityAsync(pe2);

        // Act
        List<Person> people = await _personService.GetByEpisodeId(episode.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, people.Count);
        Assert.Contains(people, p => p.Name == "Person A");
        Assert.Contains(people, p => p.Name == "Person B");
    }

    #endregion
}
