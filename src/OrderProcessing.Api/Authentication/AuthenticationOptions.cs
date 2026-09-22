namespace OrderProcessing.Api.Authentication;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string ClientId { get; set; } = "order-processing-web";

    public bool RequireHttpsMetadata { get; set; }
}
