using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Orders.ProcessOrderCreated;
using OrderProcessing.Application.Serialization;

namespace OrderProcessing.Infrastructure.Messaging.Kafka;

public sealed class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<OrderCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            ClientId = $"{_options.ClientId}-consumer",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnablePartitionEof = false
        }).Build();

        consumer.Subscribe(_options.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;

            try
            {
                result = consumer.Consume(stoppingToken);
                if (result?.Message is null)
                {
                    continue;
                }

                var message = EventJson.Deserialize<OrderCreatedV1>(result.Message.Value)
                    ?? throw new InvalidOperationException("Unable to deserialize OrderCreatedV1.");

                EnsureEventIdConsistency(result.Message, message);

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessOrderCreatedHandler>();
                await handler.HandleAsync(message, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kafka consumer failed to process message at {TopicPartitionOffset}",
                    result?.TopicPartitionOffset);
            }
        }

        consumer.Close();
    }

    private static void EnsureEventIdConsistency(Message<string, string> kafkaMessage, OrderCreatedV1 payload)
    {
        var headerEventId = ReadHeader(kafkaMessage, "event-id");
        if (Guid.TryParse(headerEventId, out var headerId) && headerId != payload.EventId)
        {
            throw new InvalidOperationException("Kafka event-id header does not match payload EventId.");
        }

        var eventType = ReadHeader(kafkaMessage, "event-type");
        if (!string.IsNullOrWhiteSpace(eventType) && eventType != OrderCreatedV1.EventType)
        {
            throw new InvalidOperationException($"Unexpected event-type '{eventType}'.");
        }
    }

    private static string? ReadHeader(Message<string, string> message, string key)
    {
        var header = message.Headers?.FirstOrDefault(item => item.Key == key);
        return header is null ? null : Encoding.UTF8.GetString(header.GetValueBytes());
    }
}
