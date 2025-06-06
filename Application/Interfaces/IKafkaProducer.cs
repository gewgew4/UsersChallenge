using Common.Dtos;

namespace Application.Interfaces;

public interface IKafkaProducer
{
    Task<bool> ProduceAsync(KafkaMessageDto message);

    Task EnsureTopicExistsAsync();
}