namespace OrderProcessing.Domain.Entities;

public sealed class OrderProcessingAttempt
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    public bool? Success { get; private set; }
    public string? ErrorMessage { get; private set; }

    private OrderProcessingAttempt()
    {
    }

    public static OrderProcessingAttempt Start(Guid orderId, int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber));
        }

        return new OrderProcessingAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AttemptNumber = attemptNumber,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static OrderProcessingAttempt Rehydrate(
        Guid id,
        Guid orderId,
        int attemptNumber,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? finishedAtUtc,
        bool? success,
        string? errorMessage)
    {
        return new OrderProcessingAttempt
        {
            Id = id,
            OrderId = orderId,
            AttemptNumber = attemptNumber,
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = finishedAtUtc,
            Success = success,
            ErrorMessage = errorMessage
        };
    }

    public void Complete(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = success ? null : errorMessage;
        FinishedAtUtc = DateTimeOffset.UtcNow;
    }
}
