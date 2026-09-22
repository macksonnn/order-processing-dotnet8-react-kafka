namespace OrderProcessing.Infrastructure.Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "orders.created.v1";

    public string ConsumerGroup { get; set; } = "order-processing-v1";

    public string ClientId { get; set; } = "order-processing-api";
}
