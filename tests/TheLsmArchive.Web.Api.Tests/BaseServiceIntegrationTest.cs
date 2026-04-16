using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Web.Api.Tests;

public abstract class BaseServiceIntegrationTest : IAsyncLifetime
{
    private readonly ServiceIntegrationTestFixture _fixture;

    public BaseServiceIntegrationTest(ServiceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        DbContext = _fixture.CreateDbContext();
        WriteDbContext = _fixture.CreateDbContext();
    }

    protected LsmArchiveDbContext DbContext { get; }

    protected LsmArchiveDbContext WriteDbContext { get; }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await WriteDbContext.DisposeAsync();
    }

    protected async Task InsertSingleInstanceOfEntityAsync<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        using LsmArchiveDbContext context = _fixture.CreateDbContext();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();
    }
}
