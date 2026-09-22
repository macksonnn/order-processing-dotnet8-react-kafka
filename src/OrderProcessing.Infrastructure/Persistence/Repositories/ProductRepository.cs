using Dapper;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Infrastructure.Persistence.Connections;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly NpgsqlSession _session;

    public ProductRepository(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "Name", "Price", "CreatedAtUtc"
            FROM "Products"
            ORDER BY "Name"
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProductRow>(new CommandDefinition(
            sql,
            transaction: _session.Transaction,
            cancellationToken: cancellationToken));

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT "Id", "Name", "Price", "CreatedAtUtc"
            FROM "Products"
            WHERE "Id" = ANY(@Ids)
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProductRow>(new CommandDefinition(
            sql,
            new { Ids = productIds.ToArray() },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return rows.Select(Map).ToList();
    }

    private static Product Map(ProductRow row) =>
        Product.Rehydrate(row.Id, row.Name, row.Price, Utc.FromDatabase(row.CreatedAtUtc));

    private sealed record ProductRow(Guid Id, string Name, decimal Price, DateTime CreatedAtUtc);
}
