namespace OrderProcessing.Infrastructure.Identity;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = "order-processing-web";

    public string? ClientSecret { get; set; }
}
