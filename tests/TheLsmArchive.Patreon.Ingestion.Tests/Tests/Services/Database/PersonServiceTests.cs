using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services.Database;
using TheLsmArchive.Patreon.Ingestion.Tests.Infrastructure;

namespace TheLsmArchive.Patreon.Ingestion.Tests.Tests.Services.Database;

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
        PersonEntity person = await personService.GetOrCreateAsync("Alice Smith", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        NotNull(person);
        True(person.Id > 0);
        Equal("Alice Smith", person.Name);
        NotNull(person.NormalizedName);

        PersonEntity? persisted = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);

        NotNull(persisted);
        Equal("Alice Smith", persisted.Name);
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
        PersonEntity result = await personService.GetOrCreateAsync("Bob Jones", cancellationToken);

        // Assert
        Equal(existing.Id, result.Id);

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
        PersonEntity result = await personService.GetOrCreateAsync("Charlie  Brown", cancellationToken);

        // Assert
        Equal(existing.Id, result.Id);
    }

    [Fact]
    public async Task GetOrCreate_WithLeadingAndTrailingWhitespace_TrimsNameBeforeProcessing()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();
        PersonService personService = Get<PersonService>();

        // Act
        PersonEntity person = await personService.GetOrCreateAsync("  Diana Prince  ", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        Equal("Diana Prince", person.Name);
    }
}
