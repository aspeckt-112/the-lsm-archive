using Microsoft.Extensions.DependencyInjection;

using TheLsmArchive.Domain.Services;
using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Domain.Tests;

/// <summary>
/// Domain test fixture that adds Domain-specific services to the shared database test infrastructure.
/// </summary>
public sealed class IntegrationTestFixture : DatabaseIntegrationTestFixture, IAsyncLifetime
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddSingleton<ShowService>();
    }
}
