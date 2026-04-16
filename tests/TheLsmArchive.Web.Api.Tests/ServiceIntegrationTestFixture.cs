using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using Respawn;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Web.Api.Tests;

public class ServiceIntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<LsmArchiveDbContext>? _dbContextOptions;

    private LsmArchiveDbContext? _respawnDbContext;

    private DbConnection? _respawnConnection;

    private Respawner? _respawner;

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        string? connectionString = _postgreSqlContainer.GetConnectionString();

        _dbContextOptions =
            new DbContextOptionsBuilder<LsmArchiveDbContext>()
                .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
                .Options;

        using LsmArchiveDbContext dbContext = new(_dbContextOptions);
        await dbContext.Database.EnsureCreatedAsync();

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
        throw new InvalidOperationException("The DbContextOptions have not been initialized."));
}
