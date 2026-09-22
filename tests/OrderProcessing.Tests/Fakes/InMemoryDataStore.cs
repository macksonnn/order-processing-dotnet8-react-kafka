using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Tests.Fakes;

public sealed class InMemoryDataStore
{
    public InMemoryState Committed { get; } = new();

    public InMemoryState? Working { get; private set; }

    public InMemoryState Current => Working ?? Committed;

    public void Begin()
    {
        Working = Committed.Clone();
    }

    public void Commit()
    {
        if (Working is null)
        {
            return;
        }

        Committed.ReplaceFrom(Working);
        Working = null;
    }

    public void Rollback()
    {
        Working = null;
    }
}

public sealed class InMemoryState
{
    public List<Product> Products { get; } = [];
    public List<Order> Orders { get; } = [];
    public List<OutboxMessage> Outbox { get; } = [];
    public List<InboxMessage> Inbox { get; } = [];
    public List<OrderProcessingAttempt> Attempts { get; } = [];

    public InMemoryState Clone()
    {
        var clone = new InMemoryState();
        clone.ReplaceFrom(this);
        return clone;
    }

    public void ReplaceFrom(InMemoryState other)
    {
        Products.Clear();
        Products.AddRange(other.Products);
        Orders.Clear();
        Orders.AddRange(other.Orders);
        Outbox.Clear();
        Outbox.AddRange(other.Outbox);
        Inbox.Clear();
        Inbox.AddRange(other.Inbox);
        Attempts.Clear();
        Attempts.AddRange(other.Attempts);
    }
}
