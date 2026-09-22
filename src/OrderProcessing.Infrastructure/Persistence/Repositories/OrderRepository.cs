using Dapper;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Persistence.Connections;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly NpgsqlSession _session;

    public OrderRepository(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        const string insertOrder = """
            INSERT INTO "Orders" ("Id", "UserId", "Status", "TotalAmount", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @UserId, @Status, @TotalAmount, @CreatedAtUtc, @UpdatedAtUtc)
            """;

        const string insertItem = """
            INSERT INTO "OrderItems"
                ("Id", "OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity", "TotalPrice")
            VALUES
                (@Id, @OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity, @TotalPrice)
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            insertOrder,
            new
            {
                order.Id,
                order.UserId,
                Status = order.Status.ToString(),
                order.TotalAmount,
                order.CreatedAtUtc,
                order.UpdatedAtUtc
            },
            _session.Transaction,
            cancellationToken: cancellationToken));

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertItem,
                item,
                _session.Transaction,
                cancellationToken: cancellationToken));
        }
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        const string orderSql = """
            SELECT "Id", "UserId", "Status", "TotalAmount", "CreatedAtUtc", "UpdatedAtUtc"
            FROM "Orders"
            WHERE "Id" = @OrderId
            """;

        const string itemsSql = """
            SELECT "Id", "OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity", "TotalPrice"
            FROM "OrderItems"
            WHERE "OrderId" = @OrderId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);

        var orderRow = await connection.QuerySingleOrDefaultAsync<OrderRow>(new CommandDefinition(
            orderSql,
            new { OrderId = orderId },
            _session.Transaction,
            cancellationToken: cancellationToken));

        if (orderRow is null)
        {
            return null;
        }

        var itemRows = await connection.QueryAsync<OrderItemRow>(new CommandDefinition(
            itemsSql,
            new { OrderId = orderId },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return Map(orderRow, itemRows);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string countSql = @"SELECT COUNT(*) FROM ""Orders""";

        const string pageSql = """
            SELECT "Id", "UserId", "Status", "TotalAmount", "CreatedAtUtc", "UpdatedAtUtc"
            FROM "Orders"
            ORDER BY "CreatedAtUtc" DESC
            OFFSET @Offset LIMIT @PageSize
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            transaction: _session.Transaction,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<OrderRow>(new CommandDefinition(
            pageSql,
            new { Offset = (page - 1) * pageSize, PageSize = pageSize },
            _session.Transaction,
            cancellationToken: cancellationToken));

        var orders = rows.Select(row => Map(row, [])).ToList();
        return (orders, total);
    }

    public async Task<bool> TryMarkStatusAsync(
        Guid orderId,
        OrderStatus expectedStatus,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "Orders"
            SET "Status" = @TargetStatus,
                "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "Id" = @OrderId
              AND "Status" = @ExpectedStatus
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                OrderId = orderId,
                ExpectedStatus = expectedStatus.ToString(),
                TargetStatus = targetStatus.ToString(),
                UpdatedAtUtc = DateTimeOffset.UtcNow
            },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return affected == 1;
    }

    private static Order Map(OrderRow row, IEnumerable<OrderItemRow> items)
    {
        return Order.Rehydrate(
            row.Id,
            row.UserId,
            Enum.Parse<OrderStatus>(row.Status),
            row.TotalAmount,
            Utc.FromDatabase(row.CreatedAtUtc),
            Utc.FromDatabase(row.UpdatedAtUtc),
            items.Select(item => OrderItem.Rehydrate(
                item.Id,
                item.OrderId,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.TotalPrice)));
    }

    private sealed record OrderRow(
        Guid Id,
        string UserId,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    private sealed record OrderItemRow(
        Guid Id,
        Guid OrderId,
        Guid ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice);
}
