using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Testing.Database;

/// <summary>
/// Shared Postgres-backed integration test fixture that manages container lifecycle,
/// schema migration, dependency injection, and database reset behavior.
/// </summary>
public abstract class DatabaseIntegrationTestFixture
{
    private readonly PostgreSqlContainer _postgresSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<LsmArchiveDbContext>? _dbContextOptions;

    private ServiceProvider? _serviceProvider;

    private LsmArchiveDbContext? _resetDbContext;

    private DbConnection? _resetConnection;

    private Respawner? _respawner;

    /// <summary>
    /// Gets the service provider built for the current test assembly's registrations.
    /// </summary>
    public IServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException("The integration test fixture has not been initialized.");

    /// <summary>
    /// Gets the container connection string once the fixture has started.
    /// </summary>
    protected string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new dependency injection scope from the fixture service provider.
    /// </summary>
    public IServiceScope CreateScope() => Services.CreateScope();

    /// <summary>
    /// Creates a new DbContext instance using the fixture's configured options.
    /// </summary>
    public LsmArchiveDbContext CreateDbContext()
    {
        DbContextOptions<LsmArchiveDbContext> dbContextOptions = _dbContextOptions
            ?? throw new InvalidOperationException("The integration test fixture has not been initialized.");

        return new LsmArchiveDbContext(dbContextOptions);
    }

    /// <summary>
    /// Starts the test database container, applies migrations, builds the service provider,
    /// and prepares the shared database reset state.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _postgresSqlContainer.StartAsync();

        ConnectionString = _postgresSqlContainer.GetConnectionString();
        _dbContextOptions = CreateDbContextOptions(ConnectionString);

        await using LsmArchiveDbContext migrationDbContext = CreateDbContext();
        await migrationDbContext.Database.MigrateAsync();

        IServiceCollection services = new ServiceCollection();
        ConfigureDatabaseServices(services, ConnectionString);
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider(CreateServiceProviderOptions());

        _resetDbContext = CreateDbContext();
        _resetConnection = _resetDbContext.Database.GetDbConnection();

        await _resetConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        });
    }

    /// <summary>
    /// Disposes the reset connection, reset DbContext, service provider, and database container.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_resetConnection is not null)
        {
            await _resetConnection.DisposeAsync();
        }

        if (_resetDbContext is not null)
        {
            await _resetDbContext.DisposeAsync();
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _postgresSqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Resets the database back to an empty application state between tests.
    /// </summary>
    public Task ResetToEmptyStateAsync()
    {
        Respawner respawner = _respawner
            ?? throw new InvalidOperationException("The integration test fixture has not been initialized.");

        DbConnection resetConnection = _resetConnection
            ?? throw new InvalidOperationException("The integration test fixture has not been initialized.");

        return respawner.ResetAsync(resetConnection);
    }

    /// <summary>
    /// Registers the shared DbContext against the service collection.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="connectionString">The running test database connection string.</param>
    protected virtual void ConfigureDatabaseServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LsmArchiveDbContext>(
            options => global::TheLsmArchive.Database.Extensions.ConfigureDbContextOptions(options, connectionString),
            ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Allows each test assembly to register the non-database services it needs against the shared infrastructure.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Creates the service provider options used when building the fixture's service provider.
    /// </summary>
    /// <returns>The service provider options.</returns>
    protected virtual ServiceProviderOptions CreateServiceProviderOptions() =>
        new() { ValidateOnBuild = true, ValidateScopes = true };

    private static DbContextOptions<LsmArchiveDbContext> CreateDbContextOptions(string connectionString)
    {
        DbContextOptionsBuilder<LsmArchiveDbContext> optionsBuilder = new();
        global::TheLsmArchive.Database.Extensions.ConfigureDbContextOptions(optionsBuilder, connectionString);
        return optionsBuilder.Options;
    }
}