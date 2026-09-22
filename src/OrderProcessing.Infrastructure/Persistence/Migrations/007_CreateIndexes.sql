CREATE INDEX IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Orders_CreatedAtUtc" ON "Orders" ("CreatedAtUtc" DESC);
CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderProcessingAttempts_OrderId" ON "OrderProcessingAttempts" ("OrderId");
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_Unpublished"
    ON "OutboxMessages" ("OccurredAtUtc")
    WHERE "PublishedAtUtc" IS NULL;
CREATE INDEX IF NOT EXISTS "IX_InboxMessages_OrderId" ON "InboxMessages" ("OrderId");
