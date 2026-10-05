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
}
