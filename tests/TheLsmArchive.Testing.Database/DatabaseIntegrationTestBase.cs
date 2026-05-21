using Microsoft.Extensions.DependencyInjection;

namespace TheLsmArchive.Testing.Database;

/// <summary>
/// Shared base class for Postgres-backed integration tests.
/// </summary>
/// <typeparam name="TFixture">The concrete fixture type for the test assembly.</typeparam>
public abstract class DatabaseIntegrationTestBase<TFixture>(TFixture fixture)
    where TFixture : DatabaseIntegrationTestFixture
{
    private IServiceScope? _scope;

    private IServiceScope Scope => _scope
        ?? throw new InvalidOperationException("The test scope has not been initialized. Ensure that InitializeAsync has been called.");

    protected TService Get<TService>() where TService : class => Scope.ServiceProvider.GetRequiredService<TService>();

    /// <summary>
    /// Creates a service scope for the test and runs the <see cref="InitializeAsyncCore"/> method.
    /// </summary>
    public virtual async ValueTask InitializeAsync()
    {
        _scope = fixture.CreateScope();
        await InitializeAsyncCore();
    }

    /// <summary>
    /// Runs any derived-class cleanup, disposes the per-test scope, and then resets the shared database state.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            await DisposeAsyncCore();
        }
        finally
        {
            _scope?.Dispose();
            _scope = null;
            await fixture.ResetToEmptyStateAsync();
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
