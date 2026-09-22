namespace OrderProcessing.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be greater than zero.");
        }

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            TotalPrice = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero)
        };
    }

    public static OrderItem Rehydrate(
        Guid id,
        Guid orderId,
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity,
        decimal totalPrice)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            TotalPrice = totalPrice
        };
    }
}
