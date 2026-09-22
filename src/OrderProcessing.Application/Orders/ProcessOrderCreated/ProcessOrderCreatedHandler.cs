using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Abstractions.Integrations;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Application.Exceptions;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Orders.ProcessOrderCreated;

public sealed class ProcessOrderCreatedHandler
{
    private readonly IInboxRepository _inbox;
    private readonly IOrderRepository _orders;
    private readonly IOrderProcessingAttemptRepository _attempts;
    private readonly IExternalOrderIntegration _external;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<ProcessOrderCreatedHandler> _logger;

    public ProcessOrderCreatedHandler(
        IInboxRepository inbox,
        IOrderRepository orders,
        IOrderProcessingAttemptRepository attempts,
        IExternalOrderIntegration external,
        IUnitOfWork unitOfWork,
        ICorrelationIdAccessor correlationId,
        ILogger<ProcessOrderCreatedHandler> logger)
    {
        _inbox = inbox;
        _orders = orders;
        _attempts = attempts;
        _external = external;
        _unitOfWork = unitOfWork;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCreatedV1 message, CancellationToken cancellationToken)
    {
        _correlationId.CorrelationId = message.CorrelationId;

        _logger.LogInformation(
            "Kafka message received {OrderId} {EventId} {CorrelationId}",
            message.OrderId,
            message.EventId,
            message.CorrelationId);

        if (message.Version != OrderCreatedV1.CurrentVersion)
        {
            throw new ValidationAppException(
                "version",
                $"Unsupported event version '{message.Version}' for {OrderCreatedV1.EventType}.");
        }

        var existing = await _inbox.GetByEventIdAsync(message.EventId, cancellationToken);
        if (existing is { IsProcessed: true })
        {
            _logger.LogInformation(
                "Duplicate message ignored {OrderId} {EventId} {CorrelationId}",
                message.OrderId,
                message.EventId,
                message.CorrelationId);
            return;
        }

        Guid? attemptId = null;
        var shouldCallExternal = true;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _inbox.TryInsertReceivedAsync(
                new InboxMessage
                {
                    EventId = message.EventId,
                    EventType = OrderCreatedV1.EventType,
                    OrderId = message.OrderId,
                    ReceivedAtUtc = DateTimeOffset.UtcNow,
                    Status = InboxStatuses.Received
                },
                ct);

            var claimed = await _orders.TryMarkStatusAsync(
                message.OrderId,
                OrderStatus.Pending,
                OrderStatus.Processing,
                ct);

            if (!claimed)
            {
                var order = await _orders.GetByIdAsync(message.OrderId, ct);
                if (order is null)
                {
                    await _inbox.MarkProcessedAsync(message.EventId, "Order not found.", ct);
                    shouldCallExternal = false;
                    return;
                }

                if (order.Status is OrderStatus.Completed or OrderStatus.Failed)
                {
                    await _inbox.MarkProcessedAsync(message.EventId, null, ct);
                    shouldCallExternal = false;
                    _logger.LogInformation(
                        "Duplicate message ignored {OrderId} {EventId} {CorrelationId}",
                        message.OrderId,
                        message.EventId,
                        message.CorrelationId);
                    return;
                }

                // Order already Processing: sequential redelivery after TX #1 committed.
                // Inbox EventId is not processed yet, so we resume instead of treating this as a duplicate.
            }

            var attemptNumber = await _attempts.GetNextAttemptNumberAsync(message.OrderId, ct);
            var attempt = OrderProcessingAttempt.Start(message.OrderId, attemptNumber);
            await _attempts.AddAsync(attempt, ct);
            attemptId = attempt.Id;
        }, cancellationToken);

        if (!shouldCallExternal || attemptId is null)
        {
            return;
        }

        _logger.LogInformation(
            "Order processing started {OrderId} {EventId} {CorrelationId}",
            message.OrderId,
            message.EventId,
            message.CorrelationId);

        var result = await InvokeExternalAsync(message, cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _attempts.CompleteAsync(attemptId.Value, result.Success, result.ErrorMessage, ct);

            if (result.Success)
            {
                await _orders.TryMarkStatusAsync(
                    message.OrderId,
                    OrderStatus.Processing,
                    OrderStatus.Completed,
                    ct);

                _logger.LogInformation(
                    "Order completed {OrderId} {EventId} {CorrelationId}",
                    message.OrderId,
                    message.EventId,
                    message.CorrelationId);
            }
            else
            {
                await _orders.TryMarkStatusAsync(
                    message.OrderId,
                    OrderStatus.Processing,
                    OrderStatus.Failed,
                    ct);

                _logger.LogWarning(
                    "Order failed {OrderId} {EventId} {CorrelationId}",
                    message.OrderId,
                    message.EventId,
                    message.CorrelationId);
            }

            await _inbox.MarkProcessedAsync(message.EventId, result.ErrorMessage, ct);
        }, cancellationToken);
    }

    private async Task<ExternalProcessingResult> InvokeExternalAsync(
        OrderCreatedV1 message,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _external.ProcessAsync(
                message.OrderId,
                message.EventId.ToString(),
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "External integration succeeded {OrderId} {EventId} {CorrelationId}",
                    message.OrderId,
                    message.EventId,
                    message.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "External integration failed {OrderId} {EventId} {CorrelationId}",
                    message.OrderId,
                    message.EventId,
                    message.CorrelationId);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "External integration failed {OrderId} {EventId} {CorrelationId}",
                message.OrderId,
                message.EventId,
                message.CorrelationId);

            return ExternalProcessingResult.Failure(ex.Message);
        }
    }
}
