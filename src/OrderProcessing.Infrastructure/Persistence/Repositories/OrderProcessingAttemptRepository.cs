using Dapper;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Infrastructure.Persistence.Connections;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class OrderProcessingAttemptRepository : IOrderProcessingAttemptRepository
{
    private readonly NpgsqlSession _session;

    public OrderProcessingAttemptRepository(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task<int> GetNextAttemptNumberAsync(Guid orderId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(MAX("AttemptNumber"), 0) + 1
            FROM "OrderProcessingAttempts"
            WHERE "OrderId" = @OrderId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { OrderId = orderId },
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task AddAsync(OrderProcessingAttempt attempt, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "OrderProcessingAttempts"
                ("Id", "OrderId", "AttemptNumber", "StartedAtUtc", "FinishedAtUtc", "Success", "ErrorMessage")
            VALUES
                (@Id, @OrderId, @AttemptNumber, @StartedAtUtc, @FinishedAtUtc, @Success, @ErrorMessage)
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            attempt,
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task CompleteAsync(
        Guid attemptId,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "OrderProcessingAttempts"
            SET "FinishedAtUtc" = @FinishedAtUtc,
                "Success" = @Success,
                "ErrorMessage" = @ErrorMessage
            WHERE "Id" = @AttemptId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                AttemptId = attemptId,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Success = success,
                ErrorMessage = success ? null : errorMessage
            },
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OrderProcessingAttempt>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT "Id", "OrderId", "AttemptNumber", "StartedAtUtc", "FinishedAtUtc", "Success", "ErrorMessage"
            FROM "OrderProcessingAttempts"
            WHERE "OrderId" = @OrderId
            ORDER BY "AttemptNumber"
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AttemptRow>(new CommandDefinition(
            sql,
            new { OrderId = orderId },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return rows.Select(row => OrderProcessingAttempt.Rehydrate(
            row.Id,
            row.OrderId,
            row.AttemptNumber,
            Utc.FromDatabase(row.StartedAtUtc),
            Utc.FromDatabase(row.FinishedAtUtc),
            row.Success,
            row.ErrorMessage)).ToList();
    }

    private sealed record AttemptRow(
        Guid Id,
        Guid OrderId,
        int AttemptNumber,
        DateTime StartedAtUtc,
        DateTime? FinishedAtUtc,
        bool? Success,
        string? ErrorMessage);
}
