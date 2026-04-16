using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using Respawn;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Patreon.Ingestion.Tests;

public class IngestionIntegrationTestFixture : IAsyncLifetime
{
    private sealed class TestDbContextFactory(DbContextOptions<LsmArchiveDbContext> dbContextOptions)
        : IDbContextFactory<LsmArchiveDbContext>
    {
        public LsmArchiveDbContext CreateDbContext() => new(dbContextOptions);

        public Task<LsmArchiveDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private readonly PostgreSqlContainer _postgreSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<LsmArchiveDbContext>? _dbContextOptions;

    private LsmArchiveDbContext? _respawnDbContext;

    private DbConnection? _respawnConnection;

    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        ConnectionString = _postgreSqlContainer.GetConnectionString();

        _dbContextOptions = new DbContextOptionsBuilder<LsmArchiveDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using LsmArchiveDbContext setupContext = new(_dbContextOptions);
        await setupContext.Database.EnsureCreatedAsync();

        // Enable pg_trgm extension for fuzzy matching (TrigramsSimilarity)
        await setupContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm");

        _respawnDbContext = new LsmArchiveDbContext(_dbContextOptions);
        _respawnConnection = _respawnDbContext.Database.GetDbConnection();

        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            _respawnConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"]
            });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _respawnConnection is null)
        {
            throw new InvalidOperationException("Respawn has not been initialized.");
        }

        await _respawner.ResetAsync(_respawnConnection);
    }

    public async ValueTask DisposeAsync()
    {
        if (_respawnDbContext is not null)
        {
            await _respawnDbContext.DisposeAsync();
        }

        await _postgreSqlContainer.DisposeAsync();
    }

    public LsmArchiveDbContext CreateDbContext() => new(_dbContextOptions ??
        throw new InvalidOperationException("DbContext options have not been initialized."));

    public IDbContextFactory<LsmArchiveDbContext> CreateDbContextFactory() =>
        new TestDbContextFactory(_dbContextOptions ??
            throw new InvalidOperationException("DbContext options have not been initialized."));
}
