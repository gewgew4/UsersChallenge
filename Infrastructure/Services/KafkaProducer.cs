using Application.Interfaces;
using Common.Dtos;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Services;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaSettings> settings, ILogger<KafkaProducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = "permission-api-producer"
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task<bool> ProduceAsync(KafkaMessageDto message)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message);

            var result = await _producer.ProduceAsync(_settings.TopicName,
                new Message<Null, string> { Value = messageJson });

            _logger.LogInformation("Message produced to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Operation: {Operation}",
                result.Topic, result.Partition.Value, result.Offset.Value, message.NameOperation);

            return result.Status == PersistenceStatus.Persisted;
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to Kafka. Operation: {Operation}", message.NameOperation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to Kafka. Operation: {Operation}", message.NameOperation);
        }

        return false;
    }

    public async Task EnsureTopicExistsAsync()
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers
        }).Build();

        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicExists = metadata.Topics.Any(t => t.Topic == _settings.TopicName);

            if (!topicExists)
            {
                await adminClient.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = _settings.TopicName,
                            NumPartitions = 3,
                            ReplicationFactor = 1
                        }
                    ]);

                _logger.LogInformation("Kafka topic created: {TopicName}", _settings.TopicName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring Kafka topic exists");
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
        GC.SuppressFinalize(this);
    }
}