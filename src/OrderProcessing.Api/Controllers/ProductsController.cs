using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Products.GetProducts;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly GetProductsHandler _handler;

    public ProductsController(GetProductsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var products = await _handler.HandleAsync(cancellationToken);
        return Ok(products);
    }
}
