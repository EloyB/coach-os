using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Anonieme inschrijving voor een lessenreeks.
/// Ofwel LessonId ofwel LessonSerieId is ingevuld.
/// </summary>
public class Enrollment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// Waar élke mail voor deze inschrijving heen gaat. Genormaliseerd opgeslagen
    /// (trim + lowercase). Bij een deelnemer zonder eigen adres is dit het adres van
    /// de groepsleider — die neemt de communicatie voor de hele groep op zich.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Eigen adres van de deelnemer. Null wanneer de communicatie via de
    /// contactpersoon loopt. Puur identiteit en weergave — nooit een verzendadres;
    /// gebruik daarvoor altijd <see cref="ContactEmail"/>.
    /// </summary>
    public string? StudentEmail { get; set; }
    public string? StudentPhone { get; set; }

    /// <summary>
    /// Geboortedatum van de deelnemer. Verplicht bij nieuwe inschrijvingen; nullable
    /// omdat bestaande rijen van vóór de tariefcategorieën hem niet hebben.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Tariefcategorie, afgeleid uit <see cref="DateOfBirth"/> en de leeftijdsgrens
    /// van de organisatie op het moment van inschrijven. Bewust opgeslagen in plaats
    /// van steeds herberekend: een deelnemer die tijdens de reeks 18 wordt, mag niet
    /// halverwege van tarief veranderen. Null = onbekend, valt terug op volwassentarief.
    /// </summary>
    public ParticipantCategory? Category { get; set; }

    /// <summary>Inschrijving voor een enkele les.</summary>
    public Guid? LessonId { get; set; }

    /// <summary>Inschrijving voor een volledige reeks.</summary>
    public Guid? LessonSerieId { get; set; }

    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Vanaf wanneer deze persoon meetelt als actieve leerling: het moment waarop de leerling zelf bevestigde
    /// (via de link in de mail), of bij een directe inschrijving (kamp, handmatig bevestigd) het inschrijfmoment.
    /// Bewust niet gekoppeld aan betaling/Confirmed, zodat een beheerder de teller niet kan omzeilen door cash niet als betaald te markeren.
    /// </summary>
    public DateTime? ActiveSince { get; set; }
    public string? Notes { get; set; }
    public Guid? EnrollmentGroupId { get; set; }

    /// <summary>
    /// Maximum aantal personen (inclusief de eigen groep) waarmee deze inschrijving in één tijdslot mag zitten:
    /// 1 = solo, 2 = duo, 3 = trio, 4 = groep van vier. Een groep kiest één waarde voor iedereen: de leden
    /// erven die van de leider. De automatische planning plaatst niemand in een slot met meer personen.
    /// Vervangt de vroegere vlag "open voor groepering".
    /// </summary>
    public int MaxGroupSize { get; set; } = 1;

    /// <summary>
    /// Prijsoptie die de speler koos bij mode ManualOption. Null voor automatische
    /// prijsmodellen zoals vaste prijs, groepsgrootte of tariefcategorie.
    /// </summary>
    public Guid? SelectedPriceOptionId { get; set; }

    /// <summary>
    /// Tijdstip waarop de bewaartermijn de persoonsgegevens van deze inschrijving wiste. De rij
    /// blijft enkel bestaan omdat er betalingen aan hangen (boekhouding van de club).
    /// </summary>
    public DateTime? AnonymizedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public LessonSerie? LessonSerie { get; set; }
    public EnrollmentGroup? EnrollmentGroup { get; set; }
    public ICollection<TimeSlotPreference> TimeSlotPreferences { get; set; } = new List<TimeSlotPreference>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<FormResponse> FormResponses { get; set; } = new List<FormResponse>();
}
