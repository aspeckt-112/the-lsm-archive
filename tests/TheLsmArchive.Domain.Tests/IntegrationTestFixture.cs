using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Domain.Services;

namespace TheLsmArchive.Domain.Tests;

public class IntegrationTestFixture
{
    private readonly PostgreSqlContainer _postgresSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<LsmArchiveDbContext> _dbContextOptions = null!;

    private ServiceProvider _serviceProvider = null!;

    private LsmArchiveDbContext _resetDbContext = null!;

    private DbConnection _resetConnection = null!;

    private Respawner _respawner = null!;

    public IServiceProvider Services => _serviceProvider;

    public IServiceScope CreateScope() => _serviceProvider.CreateScope();

    public LsmArchiveDbContext CreateDbContext() => new(_dbContextOptions);

    public async ValueTask InitializeAsync()
    {
        await _postgresSqlContainer.StartAsync();

        string connectionString = _postgresSqlContainer.GetConnectionString();

        _dbContextOptions = CreateDbContextOptions(connectionString);

        await using LsmArchiveDbContext dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        IServiceCollection serviceCollection = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ShowService>();

        serviceCollection.AddDbContext<LsmArchiveDbContext>(
            options => Database.Extensions.ConfigureDbContextOptions(options, connectionString),
            ServiceLifetime.Singleton);

        _serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        _resetDbContext = CreateDbContext();

        _resetConnection = _resetDbContext.Database.GetDbConnection();

        await _resetConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _resetConnection.DisposeAsync();
        await _resetDbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        await _postgresSqlContainer.DisposeAsync();
    }

    public Task ResetToEmptyStateAsync() => _respawner.ResetAsync(_resetConnection);

    private static DbContextOptions<LsmArchiveDbContext> CreateDbContextOptions(string connectionString)
    {
        DbContextOptionsBuilder<LsmArchiveDbContext> optionsBuilder = new();
        Database.Extensions.ConfigureDbContextOptions(optionsBuilder, connectionString);
        return optionsBuilder.Options;
    }
}
