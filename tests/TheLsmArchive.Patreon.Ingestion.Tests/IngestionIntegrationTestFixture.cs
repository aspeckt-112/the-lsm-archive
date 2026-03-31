using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using Respawn;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Patreon.Ingestion.Tests;

public class IngestionIntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<ReadWriteDbContext>? _readWriteDbContextOptions;

    private ReadWriteDbContext? _respawnDbContext;

    private DbConnection? _respawnConnection;

    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        ConnectionString = _postgreSqlContainer.GetConnectionString();

        _readWriteDbContextOptions = new DbContextOptionsBuilder<ReadWriteDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using ReadWriteDbContext setupContext = new(_readWriteDbContextOptions);
        await setupContext.Database.EnsureCreatedAsync();

        // Enable pg_trgm extension for fuzzy matching (TrigramsSimilarity)
        await setupContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm");

        _respawnDbContext = new ReadWriteDbContext(_readWriteDbContextOptions);
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

    public ReadWriteDbContext CreateReadWriteContext() => new(_readWriteDbContextOptions ??
        throw new InvalidOperationException("DbContext options have not been initialized."));
}
