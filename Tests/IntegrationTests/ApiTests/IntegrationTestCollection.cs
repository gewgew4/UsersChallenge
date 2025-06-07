using Xunit;

namespace Tests.IntegrationTests.ApiTests;

[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestBase>;