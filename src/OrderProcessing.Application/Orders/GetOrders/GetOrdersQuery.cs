namespace OrderProcessing.Application.Orders.GetOrders;

public sealed record GetOrdersQuery(int Page, int PageSize);

public sealed record OrderListItemDto(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
