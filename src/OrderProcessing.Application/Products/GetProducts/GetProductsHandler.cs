using OrderProcessing.Application.Abstractions.Persistence;

namespace OrderProcessing.Application.Products.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IProductRepository _products;

    public GetProductsHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<IReadOnlyList<ProductDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var products = await _products.GetAllAsync(cancellationToken);

        return products
            .Select(product => new ProductDto(product.Id, product.Name, product.Price, product.CreatedAtUtc))
            .ToList();
    }
}
