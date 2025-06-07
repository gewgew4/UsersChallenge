using Common.Dtos;
using Confluent.Kafka;
using FluentAssertions;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.Kafka;
using Xunit;

namespace Tests.IntegrationTests.ServiceTests;

public class KafkaConnectionTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafkaContainer;
    private KafkaProducer? _producer;
    private IOptions<KafkaSettings>? _settings;

    public KafkaConnectionTests()
    {
        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _kafkaContainer.StartAsync();

        _settings = Options.Create(new KafkaSettings
        {
            BootstrapServers = _kafkaContainer.GetBootstrapAddress(),
            TopicName = "permissions-connection-test",
            GroupId = "permissions-api-group-test"
        });

        var mockLogger = new Mock<ILogger<KafkaProducer>>();
        _producer = new KafkaProducer(_settings, mockLogger.Object);
    }

    [Fact]
    public async Task Connection_WhenKafkaAvailable_ShouldConnect()
    {
        // Act
        await _producer!.EnsureTopicExistsAsync();
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _kafkaContainer.GetBootstrapAddress()
        }).Build();

        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

        // Assert
        metadata.Topics.Should().Contain(t => t.Topic == "permissions-connection-test");
    }

    [Fact]
    public async Task ProduceMessage_WhenBrokerHealthy_ShouldSucceed()
    {
        // Arrange
        await _producer!.EnsureTopicExistsAsync();

        var message = new KafkaMessageDto
        {
            Id = Guid.NewGuid(),
            NameOperation = "connection-test"
        };

        // Act
        var result = await _producer.ProduceAsync(message);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Produce_Should_Fail_Quickly_With_Invalid_Broker()
    {
        // Arrange
        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9999",
            ClientId = "test-fail-producer",
            MessageTimeoutMs = 2000,
            SocketTimeoutMs = 2000,
            RetryBackoffMs = 500,
            MessageSendMaxRetries = 1,
        };

        var message = new Message<Null, string> { Value = "{\"NameOperation\":\"TestFail\"}" };

        // Act
        using var producer = new ProducerBuilder<Null, string>(config).Build();

        var ex = await Record.ExceptionAsync(async () =>
        {
            var deliveryResult = await producer.ProduceAsync("some-topic", message);
        });

        // Assert
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task ReconnectionLogic_WhenBrokerRestarted_ShouldEventuallyReconnect()
    {
        // Arrange
        await _producer!.EnsureTopicExistsAsync();

        var message = new KafkaMessageDto
        {
            Id = Guid.NewGuid(),
            NameOperation = "reconnection-test"
        };

        var mockLogger = new Mock<ILogger<KafkaProducer>>();

        // Act
        var firstResult = await _producer.ProduceAsync(message);

        _producer.Dispose();

        await _kafkaContainer.StopAsync();

        await Task.Delay(40000);

        await _kafkaContainer.DisposeAsync();

        var newKafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:latest")
            .WithPortBinding(9092, true)
            .WithCleanUp(true)
            .Build();

        await newKafkaContainer.StartAsync();

        var bootstrap = newKafkaContainer.GetBootstrapAddress();

        var config = new AdminClientConfig { BootstrapServers = bootstrap };
        using var adminClient = new AdminClientBuilder(config).Build();

        var kafkaReady = false;
        for (int i = 0; i < 6; i++)
        {
            try
            {
                var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                if (meta.Brokers.Count != 0)
                {
                    kafkaReady = true;
                    break;
                }
            }
            catch
            {
                await Task.Delay(10000);
            }
        }

        var newSettings = Options.Create(new KafkaSettings
        {
            BootstrapServers = bootstrap,
            TopicName = "permissions-connection-test",
            GroupId = "permissions-api-group-test"
        });

        using var newProducer = new KafkaProducer(newSettings, mockLogger.Object);

        await newProducer.EnsureTopicExistsAsync();
        var result = await newProducer.ProduceAsync(message);

        // Assert
        firstResult.Should().BeTrue();
        kafkaReady.Should().BeTrue("Kafka should be ready after container restart");
        result.Should().BeTrue("Kafka should reconnect and accept messages after broker restart");
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        await _kafkaContainer.DisposeAsync();
    }
}