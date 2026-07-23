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
    public string StudentEmail { get; set; } = string.Empty;
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
    public string? Notes { get; set; }
    public Guid? EnrollmentGroupId { get; set; }
    public bool IsOpenToGrouping { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public LessonSerie? LessonSerie { get; set; }
    public EnrollmentGroup? EnrollmentGroup { get; set; }
    public ICollection<TimeSlotPreference> TimeSlotPreferences { get; set; } = new List<TimeSlotPreference>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<FormResponse> FormResponses { get; set; } = new List<FormResponse>();
}
