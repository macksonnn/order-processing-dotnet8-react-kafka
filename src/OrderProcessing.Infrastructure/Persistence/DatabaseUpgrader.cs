using System.Reflection;
using DbUp;
using Microsoft.Extensions.Logging;

namespace OrderProcessing.Infrastructure.Persistence;

public static class DatabaseUpgrader
{
    public static void Upgrade(string connectionString, ILogger logger)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(new DbUpLogger(logger))
            .WithTransaction()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            logger.LogError(result.Error, "Database migration failed. The API will not start.");
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }

        logger.LogInformation("Database migrations applied successfully.");
    }

    public static async Task UpgradeWithRetryAsync(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Upgrade(connectionString, logger);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Waiting for PostgreSQL before applying migrations. Attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
