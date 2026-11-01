using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Een tenniskamp/stage: een aaneengesloten periode van meerdere dagen waarvoor
/// je je eenmalig inschrijft. Geen terugkerende les; los van LessonSerie.
/// </summary>
public class Camp : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid TennisClubId { get; set; }

    /// <summary>Optioneel niveau/leeftijdsindicatie (hergebruik LessonLevel).</summary>
    public LessonLevel? Level { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>LEGACY: wordt verwijderd in de opruimtaak (vervangen door DayPrice/HalfDayPrice).</summary>
    public decimal Price { get; set; }

    /// <summary>Prijs van een hele dag (EUR). 0 = gratis (geen betaalstap).</summary>
    public decimal DayPrice { get; set; }

    /// <summary>Prijs van één dagdeel (EUR); nooit hoger dan <see cref="DayPrice"/>.</summary>
    public decimal HalfDayPrice { get; set; }

    /// <summary>Mogen deelnemers ook één dagdeel per dag kiezen?</summary>
    public bool AllowHalfDays { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Uiterste inschrijfdatum. Opgeslagen als UTC (zelfde conventie als
    /// <c>LessonSerie.RegistrationDeadline</c>); services zetten <c>DateTimeKind.Utc</c>.
    /// </summary>
    public DateTime RegistrationDeadline { get; set; }

    /// <summary>Max. aantal deelnemers per dagdeel (elke dag apart geteld); null = onbeperkt.</summary>
    public int? MaxParticipants { get; set; }

    /// <summary>Soft delete / concept-vlag.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
    public ICollection<CampPrice> Prices { get; set; } = new List<CampPrice>(); // LEGACY
    public ICollection<CampDay> Days { get; set; } = new List<CampDay>();
    public ICollection<CampEnrollment> Enrollments { get; set; } = new List<CampEnrollment>();
    public CampEnrollmentForm? EnrollmentForm { get; set; }
}
