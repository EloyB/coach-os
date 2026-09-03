using System.Globalization;

namespace CoachOS.Application.Common;

/// <summary>
/// Gedeelde validatieregels voor geboortedatums. Zowel de groepsleider als elk
/// groepslid gebruikt dezelfde regels, dus staan ze hier in plaats van twee keer
/// uitgeschreven in de validator.
/// </summary>
public static class DateOfBirthRules
{
    /// <summary>Bovengrens tegen typfouten in het jaartal (bv. 1090 i.p.v. 1990).</summary>
    public const int MaxAge = 120;

    public static bool IsParseable(string? value)
        => TryParse(value, out _);

    /// <param name="today">"Vandaag" in Europe/Brussels-tijd — via <see cref="Domain.Common.TimeProviderExtensions.GetBrusselsToday"/>, nooit <see cref="DateTime.UtcNow"/> direct.</param>
    public static bool IsNotInFuture(string? value, DateOnly today)
        => !TryParse(value, out DateOnly date) || date <= today;

    /// <param name="today">"Vandaag" in Europe/Brussels-tijd — via <see cref="Domain.Common.TimeProviderExtensions.GetBrusselsToday"/>, nooit <see cref="DateTime.UtcNow"/> direct.</param>
    public static bool IsRealistic(string? value, DateOnly today)
    {
        if (!TryParse(value, out DateOnly date)) return true; // formaatfout is een aparte melding
        int age = Domain.Common.ParticipantCategoryResolver.CalculateAge(date, today);
        return age <= MaxAge;
    }

    public static bool TryParse(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return DateOnly.TryParseExact(
            value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
