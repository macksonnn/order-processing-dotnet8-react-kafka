using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OrderProcessing.Application.Abstractions.Identity;
using OrderProcessing.Application.Abstractions.Integrations;
using OrderProcessing.Application.Abstractions.Messaging;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Infrastructure.Health;
using OrderProcessing.Infrastructure.Identity;
using OrderProcessing.Infrastructure.Integrations;
using OrderProcessing.Infrastructure.Messaging.Kafka;
using OrderProcessing.Infrastructure.Messaging.Outbox;
using OrderProcessing.Infrastructure.Persistence.Connections;
using OrderProcessing.Infrastructure.Persistence.Repositories;

namespace OrderProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<ExternalIntegrationOptions>(configuration.GetSection(ExternalIntegrationOptions.SectionName));
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

        services.AddHttpClient<IIdentityProvider, KeycloakIdentityProvider>((provider, client) =>
        {
            var authority = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value.Authority
                ?? throw new InvalidOperationException("Authentication:Authority is not configured.");

            client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<NpgsqlSession>();
        services.AddScoped<IUnitOfWork, NpgsqlUnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IOrderProcessingAttemptRepository, OrderProcessingAttemptRepository>();
        services.AddScoped<IExternalOrderIntegration, FakeExternalOrderIntegration>();

        services.AddSingleton<IEventPublisher, KafkaProducer>();
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<OrderCreatedConsumer>();
        services.AddSingleton(new PostgresHealthCheck(connectionString));

        return services;
    }
}
