using Dapper;
using OrderProcessing.Application.Abstractions.Persistence;
using OrderProcessing.Infrastructure.Persistence.Connections;

namespace OrderProcessing.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly NpgsqlSession _session;

    public OutboxRepository(NpgsqlSession session)
    {
        _session = session;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "OutboxMessages"
                ("Id", "AggregateId", "EventType", "EventVersion", "Payload", "OccurredAtUtc", "PublishedAtUtc", "PublishAttempts", "LastError")
            VALUES
                (@Id, @AggregateId, @EventType, @EventVersion, @Payload::jsonb, @OccurredAtUtc, NULL, 0, NULL)
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            message,
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimUnpublishedAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH claimed AS (
                SELECT "Id"
                FROM "OutboxMessages"
                WHERE "PublishedAtUtc" IS NULL
                ORDER BY "OccurredAtUtc"
                LIMIT @BatchSize
                FOR UPDATE SKIP LOCKED
            )
            UPDATE "OutboxMessages" AS outbox
            SET "PublishAttempts" = outbox."PublishAttempts" + 1
            FROM claimed
            WHERE outbox."Id" = claimed."Id"
            RETURNING
                outbox."Id",
                outbox."AggregateId",
                outbox."EventType",
                outbox."EventVersion",
                outbox."Payload"::text AS "Payload",
                outbox."OccurredAtUtc",
                outbox."PublishedAtUtc",
                outbox."PublishAttempts",
                outbox."LastError"
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<OutboxRow>(new CommandDefinition(
            sql,
            new { BatchSize = batchSize },
            _session.Transaction,
            cancellationToken: cancellationToken));

        return rows.Select(row => new OutboxMessage
        {
            Id = row.Id,
            AggregateId = row.AggregateId,
            EventType = row.EventType,
            EventVersion = row.EventVersion,
            Payload = row.Payload,
            OccurredAtUtc = Utc.FromDatabase(row.OccurredAtUtc),
            PublishedAtUtc = Utc.FromDatabase(row.PublishedAtUtc),
            PublishAttempts = row.PublishAttempts,
            LastError = row.LastError
        }).ToList();
    }

    private sealed record OutboxRow(
        Guid Id,
        Guid AggregateId,
        string EventType,
        int EventVersion,
        string Payload,
        DateTime OccurredAtUtc,
        DateTime? PublishedAtUtc,
        int PublishAttempts,
        string? LastError);

    public async Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "OutboxMessages"
            SET "PublishedAtUtc" = @PublishedAtUtc,
                "LastError" = NULL
            WHERE "Id" = @EventId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EventId = eventId, PublishedAtUtc = DateTimeOffset.UtcNow },
            _session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "OutboxMessages"
            SET "LastError" = @Error
            WHERE "Id" = @EventId
            """;

        var connection = await _session.GetOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EventId = eventId, Error = error },
            _session.Transaction,
            cancellationToken: cancellationToken));
    }
}
