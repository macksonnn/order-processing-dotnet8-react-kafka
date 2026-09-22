CREATE TABLE IF NOT EXISTS "Products" (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(200) NOT NULL,
    "Price" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL
);
