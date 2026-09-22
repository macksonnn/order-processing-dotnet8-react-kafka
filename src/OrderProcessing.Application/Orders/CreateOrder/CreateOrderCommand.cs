namespace OrderProcessing.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(IReadOnlyList<CreateOrderItemCommand> Items);

public sealed record CreateOrderItemCommand(Guid ProductId, int Quantity);

public sealed record CreateOrderResult(Guid OrderId, string Status);
