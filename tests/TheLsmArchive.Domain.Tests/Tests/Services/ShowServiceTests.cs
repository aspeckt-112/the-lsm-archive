using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Models;
using TheLsmArchive.Domain.Services;
using TheLsmArchive.Domain.Tests.Infrastructure;

namespace TheLsmArchive.Domain.Tests.Tests.Services;

public sealed class ShowServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreateAsync_ShowAlreadyExists_ReturnsExistingShowId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        ShowEntity existingShowEntity = new() { Name = "Test Show" };

        await dbContext.Shows.AddAsync(existingShowEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        ShowService showService = Get<ShowService>();
        Show show = await showService.GetOrCreateAsync(existingShowEntity.Name, cancellationToken);

        // Assert
        Equal(existingShowEntity.Id, show.Id);
        Equal(existingShowEntity.Name, show.Name);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShowDoesNotExist_CreatesShowAndReturnsNewShowId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Act
        ShowService showService = Get<ShowService>();
        Show show = await showService.GetOrCreateAsync("New Test Show", cancellationToken);

        // Assert
        LsmArchiveDbContext dbContext = Get<LsmArchiveDbContext>();

        Show createdShowEntity = await dbContext.Shows
            .Where(s => s.Id == show.Id)
            .Select(s => new Show(s.Id, s.Name))
            .SingleAsync(cancellationToken);

        NotNull(createdShowEntity);
        Equal("New Test Show", createdShowEntity.Name);
        Equal(show.Id, createdShowEntity.Id);
        Equal(show.Name, createdShowEntity.Name);
    }
}