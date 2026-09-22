CREATE TABLE IF NOT EXISTS "OutboxMessages" (
    "Id" uuid PRIMARY KEY,
    "AggregateId" uuid NOT NULL,
    "EventType" varchar(128) NOT NULL,
    "EventVersion" integer NOT NULL,
    "Payload" jsonb NOT NULL,
    "OccurredAtUtc" timestamptz NOT NULL,
    "PublishedAtUtc" timestamptz NULL,
    "PublishAttempts" integer NOT NULL DEFAULT 0,
    "LastError" text NULL
);
