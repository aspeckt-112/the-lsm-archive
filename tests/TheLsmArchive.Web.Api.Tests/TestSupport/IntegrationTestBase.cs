using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Web.Api.Tests.TestSupport;

/// <summary>
/// Base class for direct Web API service integration tests.
/// </summary>
[Collection<IntegrationTestCollectionDefinition>]
public abstract class IntegrationTestBase(IntegrationTestFixture fixture)
    : DatabaseIntegrationTestBase<IntegrationTestFixture>(fixture), IAsyncLifetime;
