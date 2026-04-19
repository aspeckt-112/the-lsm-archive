using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Testing.Database;

/// <summary>
/// Shared base class for Postgres-backed integration tests.
/// </summary>
/// <typeparam name="TFixture">The concrete fixture type for the test assembly.</typeparam>
public abstract class DatabaseIntegrationTestBase<TFixture>(TFixture fixture)
    where TFixture : DatabaseIntegrationTestFixture
{
    /// <summary>
    /// Gets the concrete fixture instance for the current test class.
    /// </summary>
    protected TFixture Fixture { get; } = fixture;

    /// <summary>
    /// Gets the fixture's root service provider.
    /// </summary>
    protected IServiceProvider Services => Fixture.Services;

    /// <summary>
    /// Creates a new scope from the fixture service provider.
    /// </summary>
    protected IServiceScope CreateScope() => Fixture.CreateScope();

    /// <summary>
    /// Creates a new DbContext using the fixture's configured options.
    /// </summary>
    protected LsmArchiveDbContext CreateDbContext() => Fixture.CreateDbContext();

    /// <summary>
    /// Resets the database back to an empty application state.
    /// </summary>
    protected Task ResetToEmptyStateAsync() => Fixture.ResetToEmptyStateAsync();

    /// <summary>
    /// Resets the shared database state and then runs any derived-class setup.
    /// </summary>
    public virtual async ValueTask InitializeAsync()
    {
        await Fixture.ResetToEmptyStateAsync();
        await InitializeAsyncCore();
    }

    /// <summary>
    /// Runs any derived-class cleanup and then resets the shared database state.
    /// </summary>
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

    /// <summary>
    /// Allows derived tests to perform custom setup after the shared reset completes.
    /// </summary>
    /// <returns>A task representing the asynchronous setup work.</returns>
    protected virtual ValueTask InitializeAsyncCore() => ValueTask.CompletedTask;

    /// <summary>
    /// Allows derived tests to perform custom cleanup before the shared reset runs.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup work.</returns>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}