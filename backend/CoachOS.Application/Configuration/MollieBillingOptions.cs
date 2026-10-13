namespace CoachOS.Application.Configuration;

/// <summary>
/// Configuratie voor platform-billing via Mollie (clubs betalen hun CoachOS-abonnement).
/// Bind aan de <c>MollieBilling:</c> sectie. De API-key is een statische key van ons
/// eigen Mollie-account (<c>test_xxx</c> of <c>live_xxx</c>), los van de Connect-OAuth
/// credentials in <see cref="MollieOptions"/>.
///
/// Zolang <see cref="ApiKey"/> leeg is, draait een lokale fake-client (geen echte calls),
/// zodat de flow end-to-end gebouwd en getest kan worden voor de echte key er is.
/// Productie-secret: <c>MollieBilling__ApiKey</c> via Scaleway Secret Manager.
/// </summary>
public class MollieBillingOptions
{
    public const string Section = "MollieBilling";

    /// <summary>Statische Mollie API-key van ons platform-account (<c>test_</c>/<c>live_</c>). Leeg = fake.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Mollie API base URL (overschrijfbaar voor tests). Default: <c>https://api.mollie.com</c>.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.mollie.com";

    /// <summary>
    /// Publieke base URL waarop Mollie billing-webhooks zou afleveren (bv. <c>https://app.coach-os.be</c>).
    /// Optioneel in de MVP: zonder webhook volgen we de status via handmatig verversen.
    /// </summary>
    public string WebhookBaseUrl { get; set; } = string.Empty;
}
