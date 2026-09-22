using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Application.Exceptions;

namespace OrderProcessing.Application.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderProcessingAttemptRepository _attempts;

    public GetOrderByIdHandler(IOrderRepository orders, IOrderProcessingAttemptRepository attempts)
    {
        _orders = orders;
        _attempts = attempts;
    }

    public async Task<OrderDetailsDto> HandleAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken)
            ?? throw NotFoundAppException.Order(orderId);

        var attempts = await _attempts.GetByOrderIdAsync(orderId, cancellationToken);

        return new OrderDetailsDto(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Items.Select(item => new OrderItemDto(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.TotalPrice)).ToList(),
            attempts.Select(attempt => new OrderAttemptDto(
                attempt.AttemptNumber,
                attempt.StartedAtUtc,
                attempt.FinishedAtUtc,
                attempt.Success,
                attempt.ErrorMessage)).ToList());
    }
}
