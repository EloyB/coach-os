using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Per-organisatie instellingen. 1-1 relatie met <see cref="Organization"/>.
/// Nieuwe org-brede toggles worden hier toegevoegd zodat <see cref="Organization"/> beperkt blijft tot identiteit.
/// </summary>
public class OrganizationSettings : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// True wanneer de admin van deze organisatie automatisch in de trainerlijst verschijnt
    /// en lessen toegewezen kan krijgen. Default true: een tennisschool runt typisch op één
    /// admin-coach combinatie. Zet op false wanneer de admin puur administratief is.
    /// </summary>
    public bool AdminsActAsTrainers { get; set; } = true;

    /// <summary>
    /// Default <see cref="PaymentMode"/> die voorgeselecteerd wordt op nieuwe lesreeksen.
    /// Admins kunnen per lesreeks afwijken.
    /// </summary>
    public PaymentMode DefaultPaymentMode { get; set; } = PaymentMode.Immediate;

    /// <summary>
    /// Platform-commissie als percentage (0–100, met 2 decimalen, bv. 2.50 = 2,5%).
    /// Wordt enkel via super-admin gewijzigd; toegepast bij elke Mollie payment creation.
    /// </summary>
    public decimal PlatformFeePercentage { get; set; }

    /// <summary>ISO 4217 currency code voor betalingen; default <c>EUR</c>.</summary>
    public string PaymentCurrency { get; set; } = "EUR";

    /// <summary>
    /// Zet enkel bij <c>AuthService.RegisterAsync</c> wanneer een nieuwe organisatie wordt aangemaakt.
    /// NULL voor pre-existing orgs (die zien de onboarding niet) en voor lazy-geprovisioneerde rijen.
    /// Bepaalt samen met <see cref="OnboardingDismissedAt"/> of de checklist op /dashboard zichtbaar is.
    /// </summary>
    public DateTime? OnboardingStartedAt { get; set; }

    /// <summary>
    /// Gezet wanneer de admin de onboarding-checklist sluit (handmatig of via celebration state).
    /// Eenmaal gevuld verschijnt de kaart niet meer terug — ook niet als er stappen incompleet zouden raken.
    /// </summary>
    public DateTime? OnboardingDismissedAt { get; set; }

    /// <summary>
    /// Gezet zodra de admin expliciet kiest tussen solo/team in de trainer-setup stap.
    /// Onafhankelijk van <see cref="AdminsActAsTrainers"/>: een team-admin die zelf ook coacht
    /// heeft <c>AdminsActAsTrainers=true</c>; een puur administratieve team-admin heeft het op false.
    /// </summary>
    public DateTime? TrainerModeChosenAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
