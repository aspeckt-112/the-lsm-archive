using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using TheLsmArchive.Database.DbContext;

namespace TheLsmArchive.Web.Api.Tests.TestSupport;

/// <summary>
/// Custom application factory for endpoint integration tests that points the API host
/// at the shared Postgres test container managed by <see cref="IntegrationTestFixture"/>.
/// </summary>
internal sealed class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:thelsmarchive"] = _connectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LsmArchiveDbContext>();
            services.RemoveAll<DbContextOptions<LsmArchiveDbContext>>();

            services.AddDbContext<LsmArchiveDbContext>(
                options => TheLsmArchive.Database.Extensions.ConfigureDbContextOptions(options, _connectionString));
        });
    }
}


