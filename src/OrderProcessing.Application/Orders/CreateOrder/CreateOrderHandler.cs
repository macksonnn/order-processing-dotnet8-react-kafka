using Microsoft.Extensions.Logging;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Application.Exceptions;
using OrderProcessing.Application.Orders.ProcessOrderCreated;
using OrderProcessing.Application.Serialization;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler
{
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationIdAccessor _correlationId;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IProductRepository products,
        IOrderRepository orders,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICorrelationIdAccessor correlationId,
        ILogger<CreateOrderHandler> logger)
    {
        _products = products;
        _orders = orders;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _correlationId = correlationId;
        _logger = logger;
    }

    public async Task<CreateOrderResult> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        Validate(command);

        var consolidated = command.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new CreateOrderItemCommand(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        var productIds = consolidated.Select(item => item.ProductId).ToArray();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        var missing = productIds.Where(id => !productsById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationAppException(
                "items",
                $"Produto não encontrado: {string.Join(", ", missing)}.");
        }

        var invalidPrice = products.Where(product => product.Price <= 0).ToList();
        if (invalidPrice.Count > 0)
        {
            throw new ValidationAppException(
                "items",
                $"Produto com preço inválido: {string.Join(", ", invalidPrice.Select(p => p.Id))}.");
        }

        var lines = consolidated.Select(item =>
        {
            var product = productsById[item.ProductId];
            return new NewOrderItem(product.Id, product.Name, product.Price, item.Quantity);
        }).ToList();

        var order = Order.Create(_currentUser.UserId, lines);
        var createdEvent = OrderCreatedV1.Create(order.Id, _correlationId.CorrelationId);
        var outboxMessage = OutboxMessage.From(createdEvent, EventJson.Serialize(createdEvent));

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _orders.AddAsync(order, ct);
            await _outbox.AddAsync(outboxMessage, ct);
        }, cancellationToken);

        _logger.LogInformation(
            "Order created {OrderId} {EventId} {CorrelationId}",
            order.Id,
            createdEvent.EventId,
            createdEvent.CorrelationId);

        return new CreateOrderResult(order.Id, OrderStatus.Pending.ToString());
    }

    private static void Validate(CreateOrderCommand command)
    {
        if (command.Items is null || command.Items.Count == 0)
        {
            throw new ValidationAppException("items", "Informe pelo menos um item.");
        }

        if (command.Items.Any(item => item.ProductId == Guid.Empty))
        {
            throw new ValidationAppException("items", "Produto é obrigatório.");
        }

        if (command.Items.Any(item => item.Quantity <= 0))
        {
            throw new ValidationAppException("items", "Quantidade deve ser maior que zero.");
        }
    }
}
