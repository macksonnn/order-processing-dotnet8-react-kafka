using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderProcessing.Application.Abstractions.Integrations;
using OrderProcessing.Application.Orders.ProcessOrderCreated;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Tests.Fakes;

namespace OrderProcessing.Tests;

public sealed class ProcessOrderCreatedTests
{
    [Fact]
    public async Task External_integration_failure_marks_order_failed_and_attempt_unsuccessful()
    {
        var store = new InMemoryDataStore();
        var order = Order.Create("user-sub-1", [new NewOrderItem(Guid.NewGuid(), "Café", 10m, 1)]);
        store.Committed.Orders.Add(order);

        var external = Substitute.For<IExternalOrderIntegration>();
        external.ProcessAsync(order.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExternalProcessingResult.Failure("gateway timeout"));

        var handler = CreateHandler(store, external);
        var message = OrderCreatedV1.Create(order.Id, "corr-1");

        await handler.HandleAsync(message, CancellationToken.None);

        store.Committed.Orders.Single().Status.Should().Be(OrderStatus.Failed);
        store.Committed.Attempts.Should().ContainSingle();
        store.Committed.Attempts.Single().Success.Should().BeFalse();
        store.Committed.Attempts.Single().ErrorMessage.Should().Be("gateway timeout");
        store.Committed.Inbox.Single().IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task Processing_the_same_event_id_twice_invokes_external_integration_only_once()
    {
        var store = new InMemoryDataStore();
        var order = Order.Create("user-sub-1", [new NewOrderItem(Guid.NewGuid(), "Café", 10m, 1)]);
        store.Committed.Orders.Add(order);

        var external = Substitute.For<IExternalOrderIntegration>();
        external.ProcessAsync(order.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExternalProcessingResult.Ok());

        var handler = CreateHandler(store, external);
        var message = OrderCreatedV1.Create(order.Id, "corr-dup");

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        await external.Received(1).ProcessAsync(order.Id, message.EventId.ToString(), Arg.Any<CancellationToken>());
        store.Committed.Orders.Single().Status.Should().Be(OrderStatus.Completed);
        store.Committed.Attempts.Should().ContainSingle();
        store.Committed.Attempts.Single().Success.Should().BeTrue();
        store.Committed.Inbox.Single(item => item.EventId == message.EventId).IsProcessed.Should().BeTrue();
    }

    private static ProcessOrderCreatedHandler CreateHandler(
        InMemoryDataStore store,
        IExternalOrderIntegration external)
    {
        return new ProcessOrderCreatedHandler(
            new InMemoryInboxRepository(store),
            new InMemoryOrderRepository(store),
            new InMemoryAttemptRepository(store),
            external,
            new InMemoryUnitOfWork(store),
            new TestCorrelationIdAccessor(),
            NullLogger<ProcessOrderCreatedHandler>.Instance);
    }
}
