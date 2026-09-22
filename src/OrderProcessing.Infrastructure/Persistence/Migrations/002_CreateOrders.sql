CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" uuid PRIMARY KEY,
    "UserId" varchar(200) NOT NULL,
    "Status" varchar(32) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL
);
