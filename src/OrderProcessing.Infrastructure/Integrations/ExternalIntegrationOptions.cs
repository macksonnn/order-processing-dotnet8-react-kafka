namespace OrderProcessing.Infrastructure.Integrations;

public sealed class ExternalIntegrationOptions
{
    public const string SectionName = "ExternalIntegration";

    public string Mode { get; set; } = "AlwaysSuccess";

    public int MinDelaySeconds { get; set; } = 5;

    public int MaxDelaySeconds { get; set; } = 10;
}
