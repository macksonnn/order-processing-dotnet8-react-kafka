using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions.Messaging;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Application.Orders.ProcessOrderCreated;

namespace OrderProcessing.Infrastructure.Messaging.Kafka;

public sealed class KafkaProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5,
            MessageSendMaxRetries = 8,
            RetryBackoffMs = 200
        }).Build();
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var headers = new Headers
        {
            { "event-id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
            { "event-type", Encoding.UTF8.GetBytes(message.EventType) },
            { "event-version", Encoding.UTF8.GetBytes(message.EventVersion.ToString()) },
            { "correlation-id", Encoding.UTF8.GetBytes(ReadCorrelationId(message.Payload)) }
        };

        var kafkaMessage = new Message<string, string>
        {
            Key = message.AggregateId.ToString(),
            Value = message.Payload,
            Headers = headers
        };

        await _producer.ProduceAsync(_options.Topic, kafkaMessage, cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private static string ReadCorrelationId(string payload)
    {
        var parsed = Application.Serialization.EventJson.Deserialize<OrderCreatedV1>(payload);
        return parsed?.CorrelationId ?? string.Empty;
    }
}
