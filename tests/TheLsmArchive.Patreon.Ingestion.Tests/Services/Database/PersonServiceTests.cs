using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Fixtures;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Services.Database;

public sealed class PersonServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreate_WhenPersonDoesNotExist_CreatesAndReturnsNewPerson()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonService personService = Get<PersonService>();

        // Act
        int personId = await personService.GetOrCreateAsync("Alice Smith", cancellationToken);

        // Assert
        True(personId > 0);

        PersonEntity? persisted = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        NotNull(persisted);
        Equal("Alice Smith", persisted.Name);
        NotNull(persisted.NormalizedName);
    }

    [Fact]
    public async Task GetOrCreate_WhenPersonExistsByNormalizedName_ReturnsExistingPerson()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        PersonEntity existing = new() { Name = "Bob Jones", NormalizedName = "bobjones" };
        dbContext.Persons.Add(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        PersonService personService = Get<PersonService>();

        // Act — same name, normalized key is identical
        int result = await personService.GetOrCreateAsync("Bob Jones", cancellationToken);

        // Assert
        Equal(existing.Id, result);

        int count = await dbContext.Persons.CountAsync(cancellationToken);
        Equal(1, count);
    }

    [Fact]
    public async Task GetOrCreate_WhenPersonExistsByFuzzyMatch_ReturnsExistingPerson()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        PersonEntity existing = new() { Name = "Charlie Brown", NormalizedName = "charliebrown" };
        dbContext.Persons.Add(existing);
        await dbContext.SaveChangesAsync(cancellationToken);

        PersonService personService = Get<PersonService>();

        // Act — slight variation that passes trigram similarity > 0.8 but has a different normalized key
        int result = await personService.GetOrCreateAsync("Charlie  Brown", cancellationToken);

        // Assert
        Equal(existing.Id, result);
    }

    [Fact]
    public async Task GetOrCreate_WithLeadingAndTrailingWhitespace_TrimsNameBeforeProcessing()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonService personService = Get<PersonService>();

        // Act
        int personId = await personService.GetOrCreateAsync("  Diana Prince  ", cancellationToken);

        // Assert
        PersonEntity? persisted = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        NotNull(persisted);
        Equal("Diana Prince", persisted.Name);
    }
}
