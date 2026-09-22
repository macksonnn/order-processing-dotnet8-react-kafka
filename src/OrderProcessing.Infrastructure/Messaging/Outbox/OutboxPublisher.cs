using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions.Messaging;
using OrderProcessing.Application.Abstractions.Persistence;

namespace OrderProcessing.Infrastructure.Messaging.Outbox;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher loop failed.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.PollIntervalMilliseconds), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        var claimed = await ClaimAsync(cancellationToken);

        foreach (var message in claimed)
        {
            _logger.LogInformation(
                "Outbox message publishing {OrderId} {EventId}",
                message.AggregateId,
                message.Id);

            try
            {
                await using var publishScope = _scopeFactory.CreateAsyncScope();
                var publisher = publishScope.ServiceProvider.GetRequiredService<IEventPublisher>();
                await publisher.PublishAsync(message, cancellationToken);

                await MarkAsync(
                    async (outbox, ct) => await outbox.MarkPublishedAsync(message.Id, ct),
                    cancellationToken);

                _logger.LogInformation(
                    "Outbox message published {OrderId} {EventId}",
                    message.AggregateId,
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Outbox message publishing failed {OrderId} {EventId}",
                    message.AggregateId,
                    message.Id);

                await MarkAsync(
                    async (outbox, ct) => await outbox.MarkFailedAsync(message.Id, ex.Message, ct),
                    cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        IReadOnlyList<OutboxMessage> batch = [];

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            batch = await outbox.ClaimUnpublishedAsync(_options.BatchSize, ct);
        }, cancellationToken);

        return batch;
    }

    private async Task MarkAsync(
        Func<IOutboxRepository, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await action(outbox, ct);
        }, cancellationToken);
    }
}
