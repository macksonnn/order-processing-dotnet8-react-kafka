namespace OrderProcessing.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Product()
    {
    }

    public static Product Create(string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static Product Rehydrate(Guid id, string name, decimal price, DateTimeOffset createdAtUtc)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Price = price,
            CreatedAtUtc = createdAtUtc
        };
    }
}
