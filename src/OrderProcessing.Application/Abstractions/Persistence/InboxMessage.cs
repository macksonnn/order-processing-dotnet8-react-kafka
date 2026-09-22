namespace OrderProcessing.Application.Abstractions.Persistence;

public static class InboxStatuses
{
    public const string Received = "Received";
    public const string Processed = "Processed";
}

public sealed class InboxMessage
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
    public DateTimeOffset? ProcessedAtUtc { get; init; }
    public string Status { get; init; } = InboxStatuses.Received;
    public string? LastError { get; init; }

    public bool IsProcessed =>
        Status == InboxStatuses.Processed && ProcessedAtUtc is not null;
}
