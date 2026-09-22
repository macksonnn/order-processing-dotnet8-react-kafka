using OrderProcessing.Application.Orders.ProcessOrderCreated;

namespace OrderProcessing.Application.Abstractions.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public Guid AggregateId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int EventVersion { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; init; }
    public DateTimeOffset? PublishedAtUtc { get; init; }
    public int PublishAttempts { get; init; }
    public string? LastError { get; init; }

    public static OutboxMessage From(OrderCreatedV1 message, string payload)
    {
        return new OutboxMessage
        {
            Id = message.EventId,
            AggregateId = message.OrderId,
            EventType = OrderCreatedV1.EventType,
            EventVersion = message.Version,
            Payload = payload,
            OccurredAtUtc = message.OccurredAtUtc
        };
    }
}
