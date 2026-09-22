using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Application.Auth;
using OrderProcessing.Application.Orders.CreateOrder;
using OrderProcessing.Application.Orders.GetOrderById;
using OrderProcessing.Application.Orders.GetOrders;
using OrderProcessing.Application.Orders.ProcessOrderCreated;
using OrderProcessing.Application.Products.GetProducts;

namespace OrderProcessing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrdersHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<ProcessOrderCreatedHandler>();
        return services;
    }
}
