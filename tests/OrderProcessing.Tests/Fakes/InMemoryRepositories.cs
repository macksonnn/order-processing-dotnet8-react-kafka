using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Tests.Fakes;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryDataStore _store;

    public InMemoryUnitOfWork(InMemoryDataStore store)
    {
        _store = store;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        _store.Begin();

        try
        {
            await action(cancellationToken);
            _store.Commit();
        }
        catch
        {
            _store.Rollback();
            throw;
        }
    }
}

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryProductRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Product>>(_store.Current.Products.ToList());
    }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var items = _store.Current.Products.Where(product => productIds.Contains(product.Id)).ToList();
        return Task.FromResult<IReadOnlyList<Product>>(items);
    }
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryOrderRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _store.Current.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Current.Orders.SingleOrDefault(order => order.Id == orderId));
    }

    public Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var ordered = _store.Current.Orders.OrderByDescending(order => order.CreatedAtUtc).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<Order>, int)>((items, ordered.Count));
    }

    public Task<bool> TryMarkStatusAsync(
        Guid orderId,
        OrderStatus expectedStatus,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var order = _store.Current.Orders.SingleOrDefault(item => item.Id == orderId);
        if (order is null || order.Status != expectedStatus)
        {
            return Task.FromResult(false);
        }

        switch (targetStatus)
        {
            case OrderStatus.Processing:
                order.MarkAsProcessing();
                break;
            case OrderStatus.Completed:
                order.MarkAsCompleted();
                break;
            case OrderStatus.Failed:
                order.MarkAsFailed();
                break;
            default:
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

public sealed class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryOutboxRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        _store.Current.Outbox.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimUnpublishedAsync(int batchSize, CancellationToken cancellationToken)
    {
        var claimed = _store.Current.Outbox
            .Where(message => message.PublishedAtUtc is null)
            .Take(batchSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
    }

    public Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class ThrowingOutboxRepository : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Outbox insert failed.");
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimUnpublishedAsync(int batchSize, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}

public sealed class InMemoryInboxRepository : IInboxRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryInboxRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<InboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Current.Inbox.SingleOrDefault(message => message.EventId == eventId));
    }

    public Task TryInsertReceivedAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        if (_store.Current.Inbox.All(existing => existing.EventId != message.EventId))
        {
            _store.Current.Inbox.Add(message);
        }

        return Task.CompletedTask;
    }

    public Task MarkProcessedAsync(Guid eventId, string? lastError, CancellationToken cancellationToken)
    {
        var existing = _store.Current.Inbox.Single(message => message.EventId == eventId);
        _store.Current.Inbox.Remove(existing);
        _store.Current.Inbox.Add(new InboxMessage
        {
            EventId = existing.EventId,
            EventType = existing.EventType,
            OrderId = existing.OrderId,
            ReceivedAtUtc = existing.ReceivedAtUtc,
            ProcessedAtUtc = DateTimeOffset.UtcNow,
            Status = InboxStatuses.Processed,
            LastError = lastError
        });

        return Task.CompletedTask;
    }
}

public sealed class InMemoryAttemptRepository : IOrderProcessingAttemptRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryAttemptRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<int> GetNextAttemptNumberAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var next = _store.Current.Attempts.Count(attempt => attempt.OrderId == orderId) + 1;
        return Task.FromResult(next);
    }

    public Task AddAsync(OrderProcessingAttempt attempt, CancellationToken cancellationToken)
    {
        _store.Current.Attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task CompleteAsync(Guid attemptId, bool success, string? errorMessage, CancellationToken cancellationToken)
    {
        var attempt = _store.Current.Attempts.Single(item => item.Id == attemptId);
        attempt.Complete(success, errorMessage);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderProcessingAttempt>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var items = _store.Current.Attempts
            .Where(attempt => attempt.OrderId == orderId)
            .OrderBy(attempt => attempt.AttemptNumber)
            .ToList();

        return Task.FromResult<IReadOnlyList<OrderProcessingAttempt>>(items);
    }
}

public sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }
}

public sealed class TestCorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = "test-correlation";
}
