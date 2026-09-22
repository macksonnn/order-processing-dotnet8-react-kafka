using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderProcessing.Application.Abstractions.Integrations;

namespace OrderProcessing.Infrastructure.Integrations;

public sealed class FakeExternalOrderIntegration : IExternalOrderIntegration
{
    private readonly ExternalIntegrationOptions _options;
    private readonly ILogger<FakeExternalOrderIntegration> _logger;

    public FakeExternalOrderIntegration(
        IOptions<ExternalIntegrationOptions> options,
        ILogger<FakeExternalOrderIntegration> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExternalProcessingResult> ProcessAsync(
        Guid orderId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var min = Math.Max(1, _options.MinDelaySeconds);
        var max = Math.Max(min, _options.MaxDelaySeconds);
        var delaySeconds = Random.Shared.Next(min, max + 1);

        _logger.LogInformation(
            "External integration started {OrderId} with idempotency key {IdempotencyKey} and delay {DelaySeconds}s",
            orderId,
            idempotencyKey,
            delaySeconds);

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        return _options.Mode.Trim() switch
        {
            "AlwaysSuccess" => ExternalProcessingResult.Ok(),
            "AlwaysFail" => ExternalProcessingResult.Failure("Configured external integration failure."),
            "Random" => Random.Shared.Next(0, 2) == 0
                ? ExternalProcessingResult.Ok()
                : ExternalProcessingResult.Failure("Random external integration failure."),
            _ => ExternalProcessingResult.Failure($"Unknown external integration mode '{_options.Mode}'.")
        };
    }
}
