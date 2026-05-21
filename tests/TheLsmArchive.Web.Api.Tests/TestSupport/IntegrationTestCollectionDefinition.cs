namespace TheLsmArchive.Web.Api.Tests.TestSupport;

/// <summary>
/// xUnit collection definition for Web API integration tests that share the same database fixture.
/// </summary>
[CollectionDefinition]
public sealed class IntegrationTestCollectionDefinition : ICollectionFixture<IntegrationTestFixture>;
