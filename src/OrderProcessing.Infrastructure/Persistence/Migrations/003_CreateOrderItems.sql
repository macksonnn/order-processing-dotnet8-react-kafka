CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" uuid PRIMARY KEY,
    "OrderId" uuid NOT NULL REFERENCES "Orders" ("Id"),
    "ProductId" uuid NOT NULL,
    "ProductName" varchar(200) NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Quantity" integer NOT NULL,
    "TotalPrice" numeric(18,2) NOT NULL
);
