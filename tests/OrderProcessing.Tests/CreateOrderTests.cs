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
    private static readonly Guid CafeId = Guid.Parse("7c2a9e14-3b81-4f06-a9d2-1e58c0b47a2f");
    private static readonly Guid LeiteId = Guid.Parse("b4d17f88-2c09-45ae-9e31-6a0d8f52c1b7");
    private static readonly Guid BrindeId = Guid.Parse("c8a1d4e2-9b70-4f13-a582-6e3c1d90b447");

    [Fact]
    public async Task Cria_pedido_valido()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(
            new CreateOrderCommand(
            [
                new CreateOrderItemCommand(CafeId, 2),
                new CreateOrderItemCommand(LeiteId, 1)
            ]),
            CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Pending.ToString());
        store.Committed.Orders.Should().ContainSingle();

        var order = store.Committed.Orders.Single();
        order.Id.Should().Be(result.OrderId);
        order.UserId.Should().Be("user-sub-1");
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(16.47m);
        order.Items.Should().HaveCount(2);
        order.Items.Single(item => item.ProductId == CafeId).UnitPrice.Should().Be(4.99m);
        order.Items.Single(item => item.ProductId == CafeId).ProductName.Should().Be("Café 500g");

        store.Committed.Outbox.Should().ContainSingle();
        var outbox = store.Committed.Outbox.Single();
        outbox.AggregateId.Should().Be(order.Id);
        outbox.EventType.Should().Be("OrderCreatedV1");
        outbox.Payload.Should().Contain(order.Id.ToString());
    }

    [Fact]
    public async Task Rejeita_sem_itens()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var act = () => handler.HandleAsync(new CreateOrderCommand([]), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>()
            .Where(ex => ex.Errors.ContainsKey("items"));

        store.Committed.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejeita_quantidade_invalida()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var act = () => handler.HandleAsync(
            new CreateOrderCommand([new CreateOrderItemCommand(CafeId, 0)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>();
        store.Committed.Orders.Should().BeEmpty();
        store.Committed.Outbox.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejeita_preco_invalido()
    {
        var store = SeedStore();
        var handler = CreateHandler(store);

        var act = () => handler.HandleAsync(
            new CreateOrderCommand([new CreateOrderItemCommand(BrindeId, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationAppException>()
            .Where(ex => ex.Errors["items"].Any(message => message.Contains("preço inválido")));

        store.Committed.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Outbox_falhou_nao_grava_pedido()
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
            new CreateOrderCommand([new CreateOrderItemCommand(CafeId, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Outbox insert failed.");

        store.Committed.Orders.Should().BeEmpty();
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
        store.Committed.Products.Add(Product.Rehydrate(CafeId, "Café 500g", 4.99m, DateTimeOffset.UtcNow));
        store.Committed.Products.Add(Product.Rehydrate(LeiteId, "Leite 1L", 6.49m, DateTimeOffset.UtcNow));
        store.Committed.Products.Add(Product.Rehydrate(BrindeId, "Brinde", 0m, DateTimeOffset.UtcNow));
        return store;
    }
}
