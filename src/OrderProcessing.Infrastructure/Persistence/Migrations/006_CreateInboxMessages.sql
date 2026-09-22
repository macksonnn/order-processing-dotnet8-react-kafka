CREATE TABLE IF NOT EXISTS "InboxMessages" (
    "EventId" uuid PRIMARY KEY,
    "EventType" varchar(128) NOT NULL,
    "OrderId" uuid NOT NULL,
    "ReceivedAtUtc" timestamptz NOT NULL,
    "ProcessedAtUtc" timestamptz NULL,
    "Status" varchar(32) NOT NULL,
    "LastError" text NULL
);
