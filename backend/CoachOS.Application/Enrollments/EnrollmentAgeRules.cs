using CoachOS.Application.Common;
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Enrollments;

/// <summary>Leeftijdsregels die zowel de publieke als de handmatige inschrijving gebruiken.</summary>
internal static class EnrollmentAgeRules
{
    /// <summary>
    /// Controleert of elke deelnemer op de startdatum van de reeks binnen [MinAge, MaxAge] valt.
    /// Zonder bruikbare geboortedatum wordt niet geblokkeerd, consistent met de tariefcategorie
    /// en de partiële unique index. Geeft de fout voor de eerste deelnemer buiten de grens.
    /// </summary>
    public static Error? CheckSeriesAgeLimit(
        Domain.Entities.LessonSerie series, IEnumerable<(string Name, string? DateOfBirth)> people)
    {
        foreach ((string name, string? dob) in people)
        {
            if (!DateOfBirthRules.TryParse(dob, out DateOnly parsed)) continue;

            int age = ParticipantCategoryResolver.CalculateAge(parsed, series.StartDate);
            if (age < series.MinAge || age > series.MaxAge)
            {
                return new Error(ErrorCodes.Validation,
                    $"{name} ({age} jaar) valt buiten de leeftijdsgrens van deze reeks " +
                    $"({series.MinAge}–{series.MaxAge} jaar).");
            }
        }

        return null;
    }

    /// <summary>
    /// Leidt de tariefcategorie af. Zonder bruikbare geboortedatum blijft de categorie
    /// null; <c>PricingService</c> behandelt dat als volwassene.
    /// </summary>
    public static ParticipantCategory? ResolveCategory(string? dateOfBirth, int youthMaxAge, DateOnly onDate)
        => DateOfBirthRules.TryParse(dateOfBirth, out DateOnly date)
            ? ParticipantCategoryResolver.Resolve(date, youthMaxAge, onDate)
            : null;
}
