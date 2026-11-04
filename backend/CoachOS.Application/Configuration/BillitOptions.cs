namespace CoachOS.Application.Configuration;

/// <summary>
/// Configuratie voor facturatie van CoachOS-abonnementen via Billit. Bind aan de
/// <c>Billit:</c> sectie. Zolang <see cref="ApiKey"/> of <see cref="PartyId"/> leeg is, draait
/// de fake en blijft self-service buiten Development uit.
/// Productie-secrets: <c>Billit__ApiKey</c> en <c>Billit__PartyId</c> (Scaleway Secret Manager).
/// </summary>
public class BillitOptions
{
    public const string Section = "Billit";

    /// <summary>Geheime API-key uit MyBillit (profiel). Verschilt tussen sandbox en productie.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Id van het facturerende bedrijf in Billit. Verschilt tussen sandbox en productie.</summary>
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Billit API base URL. Sandbox: <c>https://api.sandbox.billit.be</c>.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.billit.be";
}
