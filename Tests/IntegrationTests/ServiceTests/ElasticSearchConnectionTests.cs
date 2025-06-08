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

public class ElasticSearchConnectionTests : IAsyncLifetime
{
    private readonly ElasticsearchContainer _elasticSearchContainer;
    private ElasticSearchService? _service;
    private ElasticClient? _client;

    public ElasticSearchConnectionTests()
    {
        _elasticSearchContainer = new ElasticsearchBuilder()
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
        await _elasticSearchContainer.StartAsync();

        var settings = Options.Create(new ElasticSearchSettings
        {
            // We're not testing HTTPS and NEST defaults to HTTP
            Uri = _elasticSearchContainer.GetConnectionString().Replace("https://", "http://"),
            IndexName = "permissions-connection-test",
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
    public async Task Connection_WhenElasticsearchAvailable_ShouldConnect()
    {
        // Arrange
        var permission = TestDataBuilders.PermissionDtoFaker.Generate();

        // Act
        await _service!.EnsureIndexExistsAsync();
        await _service.IndexPermissionAsync(permission);
        var health = await _client!.Cluster.HealthAsync();

        // Assert
        health.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task IndexOperation_WhenConnectionHealthy_ShouldSucceed()
    {
        // Arrange
        await _service!.EnsureIndexExistsAsync();
        var permission = TestDataBuilders.PermissionDtoFaker.Generate();
        permission.Id = 100;

        // Act
        await _service.IndexPermissionAsync(permission);
        await Task.Delay(1000);

        var retrieved = await _service.GetPermissionAsync(100);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(100);
    }

    [Fact]
    public async Task Connection_WhenElasticsearchUnavailable_ShouldFailGracefully()
    {
        // Arrange
        await _elasticSearchContainer.StopAsync();

        var invalidSettings = Options.Create(new ElasticSearchSettings
        {
            Uri = "http://localhost:9999",
            IndexName = "test-index",
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
    }

    [Fact]
    public async Task FailoverScenario_WhenConnectionLost_ShouldHandleGracefully()
    {
        // Arrange
        await _service!.EnsureIndexExistsAsync();
        var permission = TestDataBuilders.PermissionDtoFaker.Generate();

        // Act
        await _service.IndexPermissionAsync(permission);
        await _elasticSearchContainer.StopAsync();

        var act = async () => await _service.IndexPermissionAsync(permission);

        // Assert
        await act.Should().NotThrowAsync();
    }

    public async Task DisposeAsync()
    {
        if (_elasticSearchContainer != null)
            await _elasticSearchContainer.DisposeAsync();
    }
}