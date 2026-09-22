namespace OrderProcessing.Application.Abstractions.Persistence;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> ClaimUnpublishedAsync(int batchSize, CancellationToken cancellationToken);

    Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken);
}
