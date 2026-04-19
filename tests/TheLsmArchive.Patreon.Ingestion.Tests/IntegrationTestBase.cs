using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Patreon.Ingestion.Tests;

/// <summary>
/// Base class for Patreon ingestion integration tests.
/// </summary>
[Collection<IntegrationTestCollectionDefinition>]
public abstract class IntegrationTestBase(IntegrationTestFixture fixture)
    : DatabaseIntegrationTestBase<IntegrationTestFixture>(fixture), IAsyncLifetime;