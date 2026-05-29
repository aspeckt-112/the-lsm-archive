using Microsoft.AspNetCore.Mvc.Testing;

using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Web.Api.Tests.TestSupport;

/// <summary>
/// Base class for HTTP endpoint integration tests. This shares the same database lifecycle and
/// seeding helpers as <see cref="IntegrationTestBase"/>, while also exposing an <see cref="HttpClient"/>
/// backed by an in-memory test host.
/// </summary>
[Collection<IntegrationTestCollectionDefinition>]
public abstract class EndpointIntegrationTestBase : DatabaseIntegrationTestBase<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly string _connectionString;
    private WebApiFactory? _webApiFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointIntegrationTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The shared integration test fixture.</param>
    protected EndpointIntegrationTestBase(IntegrationTestFixture fixture)
        : base(fixture)
    {
        _connectionString = fixture.TestConnectionString;
    }

    /// <summary>
    /// Gets the configured HTTP client for the in-memory API host.
    /// </summary>
    protected HttpClient HttpClient { get; private set; } = null!;

    /// <inheritdoc />
    protected override ValueTask InitializeAsyncCore()
    {
        _webApiFactory = new WebApiFactory(_connectionString);
        HttpClient = _webApiFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        HttpClient.Dispose();

        if (_webApiFactory is not null)
        {
            await _webApiFactory.DisposeAsync();
            _webApiFactory = null;
        }
    }
}



