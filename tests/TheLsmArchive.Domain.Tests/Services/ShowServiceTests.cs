using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Domain.Models;
using TheLsmArchive.Domain.Services;

namespace TheLsmArchive.Domain.Tests.Services;

public sealed class ShowServiceTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetOrCreateAsync_ShowAlreadyExists_ReturnsExistingShowId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using IServiceScope scope = CreateScope();

        LsmArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<LsmArchiveDbContext>();

        ShowEntity existingShowEntity = new() { Name = "Test Show" };

        await dbContext.Shows.AddAsync(existingShowEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        ShowService showService = scope.ServiceProvider.GetRequiredService<ShowService>();
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

        using IServiceScope scope = CreateScope();

        // Act
        ShowService showService = scope.ServiceProvider.GetRequiredService<ShowService>();
        Show show = await showService.GetOrCreateAsync("New Test Show", cancellationToken);

        // Assert
        LsmArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<LsmArchiveDbContext>();

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