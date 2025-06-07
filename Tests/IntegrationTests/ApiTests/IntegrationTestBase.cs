using Api;
using Application.Interfaces;
using DotNet.Testcontainers.Builders;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Elasticsearch;
using Testcontainers.Kafka;
using Testcontainers.MsSql;
using Xunit;

namespace Tests.IntegrationTests.ApiTests;

public class IntegrationTestBase : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private readonly ElasticsearchContainer _elasticSearchContainer;
    private readonly KafkaContainer _kafkaContainer;

    protected WebApplicationFactory<Program>? Factory { get; private set; }
    protected HttpClient? Client { get; private set; }

    protected HttpClient TestClient => Client ?? throw new InvalidOperationException("Client not initialized. Ensure InitializeAsync has been called.");

    public IntegrationTestBase()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Express")
            .WithPortBinding(1433, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        _elasticSearchContainer = new ElasticsearchBuilder()
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.11.0")
            .WithEnvironment("discovery.type", "single-node")
            .WithEnvironment("xpack.security.enabled", "false")
            .WithEnvironment("xpack.security.enrollment.enabled", "false")
            .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
            .WithPortBinding(9200, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9200).ForPath("/_cluster/health")))
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithPortBinding(9092, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _elasticSearchContainer.StartAsync();
        await _kafkaContainer.StartAsync();

        await WaitForSqlServerAsync();
        await WaitForElasticsearchAsync();
        await WaitForKafkaAsync();

        // Depending on HW, may be necessary to wait a bit longer for services to be fully ready
        // await Task.Delay(10000);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                        ["ElasticSearch:Uri"] = $"http://localhost:{_elasticSearchContainer.GetMappedPublicPort(9200)}",
                        ["ElasticSearch:IndexName"] = "permissions-test",
                        ["ElasticSearch:Username"] = "",
                        ["ElasticSearch:Password"] = "",
                        ["Kafka:BootstrapServers"] = _kafkaContainer.GetBootstrapAddress(),
                        ["Kafka:TopicName"] = "permissions-operations-test",
                        ["Kafka:GroupId"] = "permissions-api-group-test"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(_sqlContainer.GetConnectionString()));

                    // Remove existing health checks
                    var healthCheckDescriptors = services.Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true).ToList();
                    foreach (var healthCheckDescriptor in healthCheckDescriptors)
                    {
                        services.Remove(healthCheckDescriptor);
                    }

                    // Add simplified health checks for testing
                    services.AddHealthChecks()
                        .AddDbContextCheck<ApplicationDbContext>(); // Only check database for now

                    services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                });
            });

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var elasticService = scope.ServiceProvider.GetRequiredService<IElasticSearchService>();
        await elasticService.EnsureIndexExistsAsync();

        var kafkaService = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
        await kafkaService.EnsureTopicExistsAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();

        if (_sqlContainer != null)
            await _sqlContainer.DisposeAsync();
        if (_elasticSearchContainer != null)
            await _elasticSearchContainer.DisposeAsync();
        if (_kafkaContainer != null)
            await _kafkaContainer.DisposeAsync();
    }

    private async Task WaitForSqlServerAsync()
    {
        var connectionString = _sqlContainer.GetConnectionString();
        var maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
                Console.WriteLine("SQL Server is ready");
                return;
            }
            catch (Exception ex) when (i < maxAttempts - 1)
            {
                Console.WriteLine($"SQL Server not ready (attempt {i + 1}): {ex.Message}");
                await Task.Delay(1000);
            }
        }
        throw new Exception("SQL Server failed to become ready");
    }

    private async Task WaitForElasticsearchAsync()
    {
        var elasticsearchUrl = $"http://localhost:{_elasticSearchContainer.GetMappedPublicPort(9200)}";
        var maxAttempts = 10;

        using var client = new HttpClient();
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await client.GetAsync($"{elasticsearchUrl}/_cluster/health");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Elasticsearch is ready");
                    return;
                }
            }
            catch (Exception ex) when (i < maxAttempts - 1)
            {
                Console.WriteLine($"Elasticsearch not ready (attempt {i + 1}): {ex.Message}");
                await Task.Delay(1000);
            }
        }
        throw new Exception("Elasticsearch failed to become ready");
    }

    private async Task WaitForKafkaAsync()
    {
        var bootstrapServers = _kafkaContainer.GetBootstrapAddress();
        var maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var adminClient = new Confluent.Kafka.AdminClientBuilder(
                    new Confluent.Kafka.AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata.Brokers.Count > 0)
                {
                    Console.WriteLine("Kafka is ready");
                    return;
                }
            }
            catch (Exception ex) when (i < maxAttempts - 1)
            {
                Console.WriteLine($"Kafka not ready (attempt {i + 1}): {ex.Message}");
                await Task.Delay(1000);
            }
        }
        throw new Exception("Kafka failed to become ready");
    }
}