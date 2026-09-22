namespace OrderProcessing.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalMilliseconds { get; set; } = 1000;

    public int BatchSize { get; set; } = 20;
}
