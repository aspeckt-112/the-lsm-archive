using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Testing.Database;
using TheLsmArchive.Web.Api.Features.Episodes;
using TheLsmArchive.Web.Api.Features.Persons;
using TheLsmArchive.Web.Api.Features.Search;
using TheLsmArchive.Web.Api.Features.System;
using TheLsmArchive.Web.Api.Features.Topics;

namespace TheLsmArchive.Web.Api.Tests.TestSupport;

/// <summary>
/// Web API test fixture that provides the shared Postgres-backed database infrastructure
/// and registers the API feature services for direct service integration tests.
/// </summary>
public sealed class IntegrationTestFixture : DatabaseIntegrationTestFixture, IAsyncLifetime
{
    /// <summary>
    /// Gets the running test database connection string for endpoint host configuration.
    /// </summary>
    public string TestConnectionString => ConnectionString;

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddScoped<ISearchService, SearchService>()
            .AddScoped<IEpisodeService, EpisodeService>()
            .AddScoped<IPersonService, PersonService>()
            .AddScoped<ITopicService, TopicService>()
            .AddScoped<ISystemService, SystemService>();
    }
}

