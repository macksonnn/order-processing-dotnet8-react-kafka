namespace OrderProcessing.Application.Products.GetProducts;

public sealed record ProductDto(Guid Id, string Name, decimal Price, DateTimeOffset CreatedAtUtc);
