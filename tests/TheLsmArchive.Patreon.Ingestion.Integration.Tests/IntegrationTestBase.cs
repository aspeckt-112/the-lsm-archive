using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Patreon.Ingestion.Integration.Tests;

[Collection<IntegrationTestCollectionDefinition>]
public abstract class IntegrationTestBase(IntegrationTestFixture fixture) : IAsyncLifetime
{
    protected IntegrationTestFixture Fixture { get; } = fixture;

    protected IServiceProvider Services => Fixture.Services;

    protected IServiceScope CreateScope() => Fixture.CreateScope();

    protected LsmArchiveDbContext CreateDbContext() => Fixture.CreateDbContext();

    protected Task ResetToEmptyStateAsync() => Fixture.ResetToEmptyStateAsync();

    public virtual async ValueTask InitializeAsync()
    {
        await Fixture.ResetToEmptyStateAsync();
        await InitializeAsyncCore();
    }

    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeAsyncCore();
        }
        finally
        {
            await Fixture.ResetToEmptyStateAsync();
        }
    }

    protected virtual ValueTask InitializeAsyncCore() => ValueTask.CompletedTask;

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
