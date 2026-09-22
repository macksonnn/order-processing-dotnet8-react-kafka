CREATE TABLE IF NOT EXISTS "OrderProcessingAttempts" (
    "Id" uuid PRIMARY KEY,
    "OrderId" uuid NOT NULL REFERENCES "Orders" ("Id"),
    "AttemptNumber" integer NOT NULL,
    "StartedAtUtc" timestamptz NOT NULL,
    "FinishedAtUtc" timestamptz NULL,
    "Success" boolean NULL,
    "ErrorMessage" text NULL
);
