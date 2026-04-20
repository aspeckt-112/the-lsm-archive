using TheLsmArchive.Testing.Database;

namespace TheLsmArchive.Domain.Tests.Infrastructure;

[Collection<IntegrationTestCollectionDefinition>]
public abstract class IntegrationTestBase(IntegrationTestFixture fixture)
    : DatabaseIntegrationTestBase<IntegrationTestFixture>(fixture), IAsyncLifetime;
