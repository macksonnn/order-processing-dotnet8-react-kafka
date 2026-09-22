namespace OrderProcessing.Application.Abstractions.Persistence;

public interface IInboxRepository
{
    Task<InboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task TryInsertReceivedAsync(InboxMessage message, CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid eventId, string? lastError, CancellationToken cancellationToken);
}
