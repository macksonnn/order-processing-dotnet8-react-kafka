using Dapper;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Infrastructure.Persistence.Connections;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class InboxRepository : IInboxRepository
{
    private readonly NpgsqlSession _session;

    public InboxRepository(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task<InboxMessage?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                "EventId",
                "EventType",
                "OrderId",
                "ReceivedAtUtc",
                "ProcessedAtUtc",
                "Status",
                "LastError"
            FROM "InboxMessages"
            WHERE "EventId" = @EventId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<InboxRow>(new CommandDefinition(
            sql,
            new { EventId = eventId },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return row is null
            ? null
            : new InboxMessage
            {
                EventId = row.EventId,
                EventType = row.EventType,
                OrderId = row.OrderId,
                ReceivedAtUtc = Utc.FromDatabase(row.ReceivedAtUtc),
                ProcessedAtUtc = Utc.FromDatabase(row.ProcessedAtUtc),
                Status = row.Status,
                LastError = row.LastError
            };
    }

    private sealed record InboxRow(
        Guid EventId,
        string EventType,
        Guid OrderId,
        DateTime ReceivedAtUtc,
        DateTime? ProcessedAtUtc,
        string Status,
        string? LastError);

    public async Task TryInsertReceivedAsync(InboxMessage message, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "InboxMessages"
                ("EventId", "EventType", "OrderId", "ReceivedAtUtc", "ProcessedAtUtc", "Status", "LastError")
            VALUES
                (@EventId, @EventType, @OrderId, @ReceivedAtUtc, NULL, @Status, NULL)
            ON CONFLICT ("EventId") DO NOTHING
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            message,
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task MarkProcessedAsync(Guid eventId, string? lastError, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "InboxMessages"
            SET "Status" = @Status,
                "ProcessedAtUtc" = @ProcessedAtUtc,
                "LastError" = @LastError
            WHERE "EventId" = @EventId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                EventId = eventId,
                Status = InboxStatuses.Processed,
                ProcessedAtUtc = DateTimeOffset.UtcNow,
                LastError = lastError
            },
            _session.Transaction,
            cancellationToken: cancellationToken));
    }
}
