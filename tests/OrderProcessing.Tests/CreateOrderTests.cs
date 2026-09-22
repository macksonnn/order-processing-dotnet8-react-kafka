using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderProcessing.Application.Exceptions;
using OrderProcessing.Application.Orders.CreateOrder;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Tests.Fakes;

namespace OrderProcessing.Tests;

public sealed class CreateOrderTests
{
    private static readonly Guid CoffeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MilkId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FreeSampleId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task Create_valid_order_persists_order_uses_database_prices_and_creates_outbox()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(
            new CreateOrderCommand(
            [
                new CreateOrderItemCommand(CoffeeId, 2),
                new CreateOrderItemCommand(MilkId, 1)
            ]),
            CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Pending.ToString());
        store.Committed.Orders.Should().ContainSingle();

        var order = store.Committed.Orders.Single();
        order.Id.Should().Be(result.OrderId);
        order.UserId.Should().Be("user-sub-1");
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(55.60m);
        order.Items.Should().HaveCount(2);
        order.Items.Single(item => item.ProductId == CoffeeId).UnitPrice.Should().Be(24.90m);
        order.Items.Single(item => item.ProductId == CoffeeId).ProductName.Should().Be("Café Santa Cruz 500g");

        store.Committed.Outbox.Should().ContainSingle();
        var outbox = store.Committed.Outbox.Single();
        outbox.AggregateId.Should().Be(order.Id);
        outbox.EventType.Should().Be("OrderCreatedV1");
        outbox.Id.Should().NotBeEmpty();
        outbox.Payload.Should().Contain(order.Id.ToString());
    }

    [Fact]
    public async Task Create_order_without_items_fails()
    {
        var handler = CreateHandler(SeedStore());

        var act = () => handler.HandleAsync(new CreateOrderCommand([]), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>()
            .Where(ex => ex.Errors.ContainsKey("items"));

        SeedStore().Committed.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_order_with_quantity_zero_or_negative_fails()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var act = () => handler.HandleAsync(
            new CreateOrderCommand([new CreateOrderItemCommand(CoffeeId, 0)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>();
        store.Committed.Orders.Should().BeEmpty();
        store.Committed.Outbox.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_order_with_invalid_product_price_fails()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var act = () => handler.HandleAsync(
            new CreateOrderCommand([new CreateOrderItemCommand(FreeSampleId, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>()
            .Where(ex => ex.Message.Contains("invalid price", StringComparison.OrdinalIgnoreCase)
                         || ex.Errors["items"].Any(message => message.Contains("invalid price")));

        store.Committed.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_order_rolls_back_when_outbox_insert_fails()
    {
        var store = SeedStore();
        var handler = new CreateOrderHandler(
            new InMemoryProductRepository(store),
            new InMemoryOrderRepository(store),
            new ThrowingOutboxRepository(),
            new InMemoryUnitOfWork(store),
            new TestCurrentUser("user-sub-1"),
            new TestCorrelationIdAccessor(),
            NullLogger<CreateOrderHandler>.Instance);

        var act = () => handler.HandleAsync(
            new CreateOrderCommand([new CreateOrderItemCommand(CoffeeId, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Outbox insert failed.");

        store.Committed.Orders.Should().BeEmpty("Order and OutboxMessage share the same local transaction");
        store.Committed.Outbox.Should().BeEmpty();
    }

    private static CreateOrderHandler CreateHandler(InMemoryDataStore store)
    {
        return new CreateOrderHandler(
            new InMemoryProductRepository(store),
            new InMemoryOrderRepository(store),
            new InMemoryOutboxRepository(store),
            new InMemoryUnitOfWork(store),
            new TestCurrentUser("user-sub-1"),
            new TestCorrelationIdAccessor(),
            NullLogger<CreateOrderHandler>.Instance);
    }

    private static InMemoryDataStore SeedStore()
    {
        var store = new InMemoryDataStore();
        store.Committed.Products.Add(Product.Rehydrate(CoffeeId, "Café Santa Cruz 500g", 24.90m, DateTimeOffset.UtcNow));
        store.Committed.Products.Add(Product.Rehydrate(MilkId, "Leite Integral 1L", 5.80m, DateTimeOffset.UtcNow));
        store.Committed.Products.Add(Product.Rehydrate(FreeSampleId, "Brinde", 0m, DateTimeOffset.UtcNow));
        return store;
    }
}
