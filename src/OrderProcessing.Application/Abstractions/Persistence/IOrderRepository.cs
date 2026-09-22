using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<bool> TryMarkStatusAsync(
        Guid orderId,
        OrderStatus expectedStatus,
        OrderStatus targetStatus,
        CancellationToken cancellationToken);
}
