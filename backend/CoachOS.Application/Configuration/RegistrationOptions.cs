namespace CoachOS.Application.Configuration;

/// <summary>Configuratie voor zelfregistratie. Bind aan de <c>Registration:</c> sectie.</summary>
public class RegistrationOptions
{
    public const string Section = "Registration";

    /// <summary>
    /// Markeert het e-mailadres bij registratie meteen als bevestigd en verstuurt geen code.
    /// Enkel voor Development, zodat reset + seed zonder mailbox groen loopt.
    /// </summary>
    public bool AutoVerifyEmail { get; set; }

    /// <summary>Versie van de algemene voorwaarden die bij registratie wordt vastgelegd.</summary>
    public string TermsVersion { get; set; } = "concept";

    /// <summary>Versie van de verwerkersovereenkomst die bij registratie wordt vastgelegd.</summary>
    public string DataProcessingAgreementVersion { get; set; } = "concept";

    /// <summary>
    /// Noodrem: maximaal aantal nieuwe organisaties per uur, over alle IP's samen. Daarboven
    /// weigert registratie tijdelijk, zodat een bot met veel IP's ons niet ongelimiteerd
    /// verificatiemails laat versturen.
    /// </summary>
    public int MaxRegistrationsPerHour { get; set; } = 30;

    /// <summary>Maximaal aantal registratiepogingen per IP-adres per uur (rate limiter).</summary>
    public int MaxAttemptsPerIpPerHour { get; set; } = 5;
}
