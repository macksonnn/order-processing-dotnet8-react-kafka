using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;

    private Order()
    {
    }

    public static Order Create(string userId, IReadOnlyCollection<NewOrderItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.", nameof(items));
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var item in items)
        {
            order._items.Add(OrderItem.Create(
                order.Id,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity));
        }

        order.TotalAmount = decimal.Round(order._items.Sum(i => i.TotalPrice), 2, MidpointRounding.AwayFromZero);
        return order;
    }

    public static Order Rehydrate(
        Guid id,
        string userId,
        OrderStatus status,
        decimal totalAmount,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IEnumerable<OrderItem>? items = null)
    {
        var order = new Order
        {
            Id = id,
            UserId = userId,
            Status = status,
            TotalAmount = totalAmount,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };

        if (items is not null)
        {
            order._items.AddRange(items);
        }

        return order;
    }

    public void MarkAsProcessing()
    {
        EnsureTransition(OrderStatus.Pending, OrderStatus.Processing);
        Status = OrderStatus.Processing;
        Touch();
    }

    public void MarkAsCompleted()
    {
        EnsureTransition(OrderStatus.Processing, OrderStatus.Completed);
        Status = OrderStatus.Completed;
        Touch();
    }

    public void MarkAsFailed()
    {
        EnsureTransition(OrderStatus.Processing, OrderStatus.Failed);
        Status = OrderStatus.Failed;
        Touch();
    }

    private void EnsureTransition(OrderStatus expected, OrderStatus target)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot change order {Id} from {Status} to {target}.");
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}

public sealed record NewOrderItem(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
