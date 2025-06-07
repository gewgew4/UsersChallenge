using FluentAssertions;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using Testcontainers.Elasticsearch;
using Tests.Helpers;
using Xunit;

namespace Tests.IntegrationTests.ServiceTests;

public class ElasticSearchIntegrationTests : IAsyncLifetime
{
    private readonly ElasticsearchContainer _elasticsearchContainer;
    private ElasticSearchService? _service;
    private ElasticClient? _client;

    public ElasticSearchIntegrationTests()
    {
        _elasticsearchContainer = new ElasticsearchBuilder()
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.11.0")
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "false")
            .WithEnvironment("xpack.security.enrollment.enabled", "false")
            .WithEnvironment("xpack.security.http.ssl.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _elasticsearchContainer.StartAsync();

        var settings = Options.Create(new ElasticSearchSettings
        {
            Uri = _elasticsearchContainer.GetConnectionString().Replace("https://", "http://"),
            IndexName = "permissions-integration-test",
            Username = "",
            Password = ""
        });

        var connectionSettings = new ConnectionSettings(new Uri(settings.Value.Uri))
            .DefaultIndex(settings.Value.IndexName)
            .DisableDirectStreaming()
            .EnableApiVersioningHeader(false);

        _client = new ElasticClient(connectionSettings);

        var mockLogger = new Mock<ILogger<ElasticSearchService>>();
        _service = new ElasticSearchService(_client, settings, mockLogger.Object);
    }

    [Fact]
    public async Task EnsureIndexExists_WithRealElasticsearch_ShouldCreateIndex()
    {
        // Act
        await _service!.EnsureIndexExistsAsync();
        var indexExists = await _client!.Indices.ExistsAsync("permissions-integration-test");

        // Assert
        indexExists.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task IndexAndRetrievePermission_WithRealElasticsearch_ShouldWork()
    {
        // Arrange
        await _service!.EnsureIndexExistsAsync();
        var permission = TestDataBuilders.PermissionDtoFaker.Generate();
        permission.Id = 1;

        // Act
        await _service.IndexPermissionAsync(permission);
        await Task.Delay(1000);

        var retrieved = await _service.GetPermissionAsync(permission.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(permission.Id);
        retrieved.EmployeeForename.Should().Be(permission.EmployeeForename);
    }

    [Fact]
    public async Task IndexPermission_WithInvalidIndex_ShouldHandleFailure()
    {
        // Arrange
        var invalidSettings = Options.Create(new ElasticSearchSettings
        {
            Uri = _elasticsearchContainer.GetConnectionString(),
            IndexName = "invalid-index-name-with-uppercase-INVALID",
            Username = "",
            Password = ""
        });

        var connectionSettings = new ConnectionSettings(new Uri(invalidSettings.Value.Uri))
            .DefaultIndex(invalidSettings.Value.IndexName);

        var invalidClient = new ElasticClient(connectionSettings);
        var mockLogger = new Mock<ILogger<ElasticSearchService>>();
        var invalidService = new ElasticSearchService(invalidClient, invalidSettings, mockLogger.Object);

        var permission = TestDataBuilders.PermissionDtoFaker.Generate();

        // Act
        var act = async () => await invalidService.IndexPermissionAsync(permission);

        // Assert
        await act.Should().NotThrowAsync();

        mockLogger.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to index permission")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchPermissions_WithNonExistentIndex_ShouldReturnEmpty()
    {
        // Arrange
        var nonExistentSettings = Options.Create(new ElasticSearchSettings
        {
            Uri = _elasticsearchContainer.GetConnectionString(),
            IndexName = "non-existent-index",
            Username = "",
            Password = ""
        });

        var connectionSettings = new ConnectionSettings(new Uri(nonExistentSettings.Value.Uri))
            .DefaultIndex(nonExistentSettings.Value.IndexName);

        var client = new ElasticClient(connectionSettings);
        var mockLogger = new Mock<ILogger<ElasticSearchService>>();
        var service = new ElasticSearchService(client, nonExistentSettings, mockLogger.Object);

        // Act
        var results = await service.SearchPermissionsAsync("test");

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        if (_elasticsearchContainer != null)
            await _elasticsearchContainer.DisposeAsync();
    }
}