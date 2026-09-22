using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Orders.CreateOrder;
using OrderProcessing.Application.Orders.GetOrderById;
using OrderProcessing.Application.Orders.GetOrders;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly CreateOrderHandler _createOrder;
    private readonly GetOrdersHandler _getOrders;
    private readonly GetOrderByIdHandler _getOrderById;

    public OrdersController(
        CreateOrderHandler createOrder,
        GetOrdersHandler getOrders,
        GetOrderByIdHandler getOrderById)
    {
        _createOrder = createOrder;
        _getOrders = getOrders;
        _getOrderById = getOrderById;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            (request.Items ?? []).Select(item => new CreateOrderItemCommand(item.ProductId, item.Quantity)).ToList());

        var result = await _createOrder.HandleAsync(command, cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetOrdersHandler.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _getOrders.HandleAsync(new GetOrdersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getOrderById.HandleAsync(id, cancellationToken);
        return Ok(result);
    }
}

public sealed record CreateOrderRequest(IReadOnlyList<CreateOrderItemRequest>? Items);

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
