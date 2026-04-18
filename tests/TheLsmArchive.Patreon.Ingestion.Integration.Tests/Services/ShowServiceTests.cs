using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities;
using TheLsmArchive.Patreon.Ingestion.Services.Database;

namespace TheLsmArchive.Patreon.Ingestion.Integration.Tests.Services;

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
        int showId = await showService.GetOrCreateAsync(existingShowEntity.Name, cancellationToken);

        // Assert
        Equal(existingShowEntity.Id, showId);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShowDoesNotExist_CreatesShowAndReturnsNewShowId()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using IServiceScope scope = CreateScope();

        // Act
        ShowService showService = scope.ServiceProvider.GetRequiredService<ShowService>();
        int _ = await showService.GetOrCreateAsync("New Test Show", cancellationToken);

        // Assert
        LsmArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<LsmArchiveDbContext>();
        ShowEntity? createdShowEntity = await dbContext.Shows.FirstOrDefaultAsync(cancellationToken: cancellationToken);
        NotNull(createdShowEntity);
        Equal("New Test Show", createdShowEntity.Name);

    }
}
