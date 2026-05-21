using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Database.DbContext;
using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Patreon.Ingestion.Tests;

/// <summary>
/// Patreon ingestion test fixture that adds ingestion-specific database services to the shared infrastructure.
/// </summary>
public sealed class IntegrationTestFixture : DatabaseIntegrationTestFixture, IAsyncLifetime
{
    /// <inheritdoc />
    protected override void ConfigureDatabaseServices(IServiceCollection services, string connectionString)
    {
        base.ConfigureDatabaseServices(services, connectionString);

        services.AddDbContextFactory<LsmArchiveDbContext>(
            options => Database.Extensions.ConfigureDbContextOptions(options, connectionString),
            ServiceLifetime.Scoped);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services) => services.AddLogging();
}
