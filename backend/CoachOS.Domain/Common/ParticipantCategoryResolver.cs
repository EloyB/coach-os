using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Common;

/// <summary>
/// Leidt de tariefcategorie af uit een geboortedatum. Pure domeinlogica zonder
/// afhankelijkheden, zodat zowel de inschrijfflow als de prijsberekening dezelfde
/// regel gebruiken.
/// </summary>
public static class ParticipantCategoryResolver
{
    /// <summary>
    /// Bepaalt de categorie op basis van de leeftijd op <paramref name="onDate"/>.
    /// Wie op die datum <paramref name="youthMaxAge"/> of jonger is, telt als jeugd.
    /// </summary>
    public static ParticipantCategory Resolve(
        DateOnly dateOfBirth, int youthMaxAge, DateOnly onDate)
    {
        int age = CalculateAge(dateOfBirth, onDate);
        return age <= youthMaxAge ? ParticipantCategory.Youth : ParticipantCategory.Adult;
    }

    /// <summary>
    /// Leeftijd in hele jaren op <paramref name="onDate"/>. Trekt er een jaar af
    /// wanneer de verjaardag dat jaar nog moet komen — inclusief het schrikkeljaar-
    /// geval waarbij 29 februari op een niet-schrikkeljaar valt.
    /// </summary>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly onDate)
    {
        int age = onDate.Year - dateOfBirth.Year;

        DateOnly birthdayThisYear;
        if (dateOfBirth is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(onDate.Year))
        {
            // Geen 29 februari dit jaar: verjaardag valt op 1 maart.
            birthdayThisYear = new DateOnly(onDate.Year, 3, 1);
        }
        else
        {
            birthdayThisYear = new DateOnly(onDate.Year, dateOfBirth.Month, dateOfBirth.Day);
        }

        if (onDate < birthdayThisYear) age--;
        return age < 0 ? 0 : age;
    }
}
