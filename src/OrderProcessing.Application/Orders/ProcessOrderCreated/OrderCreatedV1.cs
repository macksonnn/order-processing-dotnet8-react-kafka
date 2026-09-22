namespace OrderProcessing.Application.Orders.ProcessOrderCreated;

public sealed record OrderCreatedV1(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    int Version)
{
    public const string EventType = "OrderCreatedV1";
    public const int CurrentVersion = 1;

    public static OrderCreatedV1 Create(Guid orderId, string correlationId)
    {
        return new OrderCreatedV1(
            Guid.NewGuid(),
            orderId,
            DateTimeOffset.UtcNow,
            correlationId,
            CurrentVersion);
    }
}
