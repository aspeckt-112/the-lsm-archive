using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Database;

/// <summary>
/// Extension methods for database operations.
/// </summary>
public static class Extensions
{
    private const string ConnectionStringName = "thelsmarchive";

    /// <summary>
    /// Extends IServiceCollection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the DbContext to the service collection.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="serviceLifetime">The lifetime of the DbContext service.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
        public IServiceCollection AddDbContext(
            IConfiguration configuration,
            ServiceLifetime serviceLifetime)
        {
            string connectionString = GetConnectionStringOrThrow(configuration, ConnectionStringName);

            services.AddDbContext<LsmArchiveDbContext>(options => ConfigureDbContextOptions(options, connectionString), serviceLifetime);

            return services;
        }

        /// <summary>
        /// Adds a factory for creating DbContext instances to the service collection, allowing for more flexible lifetime management and on-demand creation of DbContext instances.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddDbContextFactory(IConfiguration configuration)
        {
            string connectionString = GetConnectionStringOrThrow(configuration, ConnectionStringName);

            services.AddDbContextFactory<LsmArchiveDbContext>(options => ConfigureDbContextOptions(options, connectionString));

            return services;
        }

        public static void ConfigureDbContextOptions(DbContextOptionsBuilder options, string connectionString)
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null));

            options.UseSnakeCaseNamingConvention();
        }
    }

    private static string GetConnectionStringOrThrow(IConfiguration configuration, string name)
    {
        string? connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string '{name}' is not configured. " +
                "Please add it to your appsettings.json or environment variables.");
        }

        return connectionString;
    }
}
