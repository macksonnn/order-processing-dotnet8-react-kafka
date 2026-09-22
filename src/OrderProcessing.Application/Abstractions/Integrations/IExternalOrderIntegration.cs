namespace OrderProcessing.Application.Abstractions.Integrations;

public sealed record ExternalProcessingResult(bool Success, string? ErrorMessage)
{
    public static ExternalProcessingResult Ok() => new(true, null);

    public static ExternalProcessingResult Failure(string errorMessage) => new(false, errorMessage);
}

public interface IExternalOrderIntegration
{
    Task<ExternalProcessingResult> ProcessAsync(
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
