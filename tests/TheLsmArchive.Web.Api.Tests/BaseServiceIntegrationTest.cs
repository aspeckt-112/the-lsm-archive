using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Database.Entities.Abstractions;

namespace TheLsmArchive.Web.Api.Tests;

public abstract class BaseServiceIntegrationTest : IAsyncLifetime
{
    private readonly ServiceIntegrationTestFixture _fixture;

    public BaseServiceIntegrationTest(ServiceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        ReadOnlyDbContext = _fixture.CreateReadOnlyContext();
        ReadWriteDbContext = _fixture.CreateReadWriteContext();
    }

    protected ReadOnlyDbContext ReadOnlyDbContext { get; }

    protected ReadWriteDbContext ReadWriteDbContext { get; }

    public async ValueTask InitializeAsync() => await Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await ReadOnlyDbContext.DisposeAsync();
        await ReadWriteDbContext.DisposeAsync();
    }

    protected async Task InsertSingleInstanceOfEntityAsync<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        using ReadWriteDbContext context = _fixture.CreateReadWriteContext();
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();
    }
}
