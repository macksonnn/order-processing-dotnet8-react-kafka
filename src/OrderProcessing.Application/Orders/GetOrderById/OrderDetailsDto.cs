namespace OrderProcessing.Application.Orders.GetOrderById;

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public sealed record OrderAttemptDto(
    int AttemptNumber,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    bool? Success,
    string? ErrorMessage);

public sealed record OrderDetailsDto(
    Guid OrderId,
    string UserId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderAttemptDto> Attempts);
