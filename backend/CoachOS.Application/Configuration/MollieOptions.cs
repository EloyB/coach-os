namespace CoachOS.Application.Configuration;

/// <summary>
/// Configuratie voor de Mollie Connect integratie. Bind aan de
/// <c>Mollie:</c> sectie. Productie-secrets komen uit Scaleway Secret
/// Manager (<c>Mollie__ClientId</c>, <c>Mollie__ClientSecret</c>,
/// <c>Mollie__WebhookBaseUrl</c>). Lokaal dev kan via
/// <c>appsettings.Development.json</c>.
/// </summary>
public class MollieOptions
{
    public const string Section = "Mollie";

    /// <summary>Mollie Connect OAuth client id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Mollie Connect OAuth client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Publieke base URL waarop Mollie webhooks aflevert (bv. <c>https://app.coach-os.be</c>).
    /// De effectieve webhook endpoint is <c>{WebhookBaseUrl}/api/webhooks/mollie</c>.
    /// Wordt door <c>PaymentService</c> in PR #4 gebruikt; nu al gebonden zodat de
    /// config-validatie in DI eenduidig blijft.
    /// </summary>
    public string WebhookBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Volledige redirect URI die in het Mollie dashboard is geregistreerd.
    /// Moet exact matchen met wat in OAuth flow wordt meegestuurd, inclusief
    /// scheme en pad. Default: leeg → wordt op runtime afgeleid van de inkomende
    /// request (<c>{scheme}://{host}/api/oauth/mollie/callback</c>).
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Mollie API base URL (overschrijfbaar voor tests). Default: <c>https://api.mollie.com</c>.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.mollie.com";

    /// <summary>Mollie OAuth dialog base URL (overschrijfbaar voor tests). Default: <c>https://my.mollie.com</c>.</summary>
    public string OAuthBaseUrl { get; set; } = "https://my.mollie.com";

    /// <summary>
    /// Komma-gescheiden lijst van scopes die in de OAuth-autorisatie-URL worden gevraagd.
    /// Defaults dekken alles wat CoachOS nodig heeft voor onboarding + payments.
    /// </summary>
    public string Scopes { get; set; } = "payments.read payments.write organizations.read profiles.read profiles.write onboarding.read";
}
