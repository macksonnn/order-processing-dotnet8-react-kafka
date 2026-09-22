using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}
