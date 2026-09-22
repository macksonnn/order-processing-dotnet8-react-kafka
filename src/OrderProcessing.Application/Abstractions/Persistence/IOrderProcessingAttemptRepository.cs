using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Abstractions.Persistence;

public interface IOrderProcessingAttemptRepository
{
    Task<int> GetNextAttemptNumberAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(OrderProcessingAttempt attempt, CancellationToken cancellationToken);

    Task CompleteAsync(Guid attemptId, bool success, string? errorMessage, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderProcessingAttempt>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
