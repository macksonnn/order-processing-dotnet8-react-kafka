using OrderProcessing.Application.Abstractions.Persistence;

namespace OrderProcessing.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
