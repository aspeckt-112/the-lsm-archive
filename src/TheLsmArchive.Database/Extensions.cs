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
            string connectionString = configuration.GetConnectionString("thelsmarchive")
                                      ?? throw new InvalidOperationException(
                                          "The connection string 'thelsmarchive' is not configured. " +
                                          "Please add it to your appsettings.json or environment variables."
                                      );

            void configureOptions(DbContextOptionsBuilder options)
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null));
                options.UseSnakeCaseNamingConvention();
            }

            services.AddDbContext<ReadOnlyDbContext>(configureOptions, serviceLifetime);
            services.AddDbContext<ReadWriteDbContext>(configureOptions, serviceLifetime);


            return services;
        }
    }
}
