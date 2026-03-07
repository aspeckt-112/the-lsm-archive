using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Web.Api.Tests;

public class ServiceIntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer =
        new PostgreSqlBuilder("postgres:13.22-alpine3.22").Build();

    private DbContextOptions<ReadOnlyDbContext>? _readOnlyDbContextOptions;

    private DbContextOptions<ReadWriteDbContext>? _readWriteDbContextOptions;

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        string? connectionString = _postgreSqlContainer.GetConnectionString();

        _readOnlyDbContextOptions =
            new DbContextOptionsBuilder<ReadOnlyDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        _readWriteDbContextOptions =
            new DbContextOptionsBuilder<ReadWriteDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        using ReadWriteDbContext readWriteDbContext = new(_readWriteDbContextOptions);
        await readWriteDbContext.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => _postgreSqlContainer.DisposeAsync();

    public ReadOnlyDbContext CreateReadOnlyContext() => new(_readOnlyDbContextOptions ??
        throw new InvalidOperationException("The ReadOnlyDbContextOptions have not been initialized."));

    public ReadWriteDbContext CreateReadWriteContext() => new(_readWriteDbContextOptions ??
        throw new InvalidOperationException("The ReadWriteDbContextOptions have not been initialized."));
}
