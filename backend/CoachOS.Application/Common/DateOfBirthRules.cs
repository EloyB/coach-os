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

    public static bool IsNotInFuture(string? value)
        => !TryParse(value, out DateOnly date) || date <= DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsRealistic(string? value)
    {
        if (!TryParse(value, out DateOnly date)) return true; // formaatfout is een aparte melding
        int age = ParticipantCategoryResolverBridge.Age(date);
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

/// <summary>
/// Dunne brug naar de domeinlogica zodat Application niet zelf leeftijd berekent.
/// </summary>
internal static class ParticipantCategoryResolverBridge
{
    public static int Age(DateOnly dateOfBirth)
        => Domain.Common.ParticipantCategoryResolver.CalculateAge(
            dateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
}
