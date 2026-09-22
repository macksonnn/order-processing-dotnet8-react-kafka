using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Application.Exceptions;

namespace OrderProcessing.Application.Orders.GetOrders;

public sealed class GetOrdersHandler
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    private readonly IOrderRepository _orders;

    public GetOrdersHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<PagedResult<OrderListItemDto>> HandleAsync(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : query.PageSize;

        if (pageSize > MaxPageSize)
        {
            throw new ValidationAppException("pageSize", $"pageSize must be between 1 and {MaxPageSize}.");
        }

        var (orders, totalCount) = await _orders.GetPagedAsync(page, pageSize, cancellationToken);

        var items = orders
            .Select(order => new OrderListItemDto(
                order.Id,
                order.Status.ToString(),
                order.TotalAmount,
                order.CreatedAtUtc))
            .ToList();

        return new PagedResult<OrderListItemDto>(items, page, pageSize, totalCount);
    }
}
