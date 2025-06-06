namespace Infrastructure.Configuration;

public class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
}