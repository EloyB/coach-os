using System.Globalization;
using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;

namespace CoachOS.Application.Camps.Validators;

/// <summary>Regels die Create- en UpdateCampRequestValidator delen (dagdelen en prijzen).</summary>
internal static class CampRequestRules
{
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    private const string DateFormat = "yyyy-MM-dd";

    public static bool IsValidTime(string? t) => t is not null && TimePattern.IsMatch(t);

    public static bool IsValidDate(string? d) =>
        d is not null && DateOnly.TryParseExact(d, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    public static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);

    /// <summary>Eerste prijsfout, of null.</summary>
    public static string? PriceError(decimal dayPrice, decimal halfDayPrice, bool allowHalfDays)
    {
        if (dayPrice < 0m) return "Prijs mag niet negatief zijn";
        if (halfDayPrice < 0m) return "Prijs mag niet negatief zijn";
        if (halfDayPrice > dayPrice) return "De prijs van een halve dag mag niet hoger zijn dan die van een hele dag";
        if (allowHalfDays && dayPrice > 0m && halfDayPrice <= 0m) return "Geef een prijs voor een halve dag";
        return null;
    }

    /// <summary>Alle fouten van één kampdag (dagdelen, uren en trainershiften).</summary>
    public static IEnumerable<string> DayErrors(CreateCampDayRequest d)
    {
        if (!IsValidDate(d.Date))
        {
            yield return "Ongeldige datum (yyyy-MM-dd)";
            yield break;
        }

        string?[] times = [d.MorningStart, d.MorningEnd, d.AfternoonStart, d.AfternoonEnd];
        if (times.Any(t => t is not null && !IsValidTime(t)))
        {
            yield return "Ongeldige tijd (HH:mm)";
            yield break;
        }

        bool morning = d.MorningStart is not null || d.MorningEnd is not null;
        bool afternoon = d.AfternoonStart is not null || d.AfternoonEnd is not null;
        if (!morning && !afternoon)
        {
            yield return "Een dag heeft minstens één dagdeel nodig";
            yield break;
        }

        if ((morning && (d.MorningStart is null || d.MorningEnd is null))
            || (afternoon && (d.AfternoonStart is null || d.AfternoonEnd is null)))
        {
            yield return "Geef van elk dagdeel een start- en eindtijd";
            yield break;
        }

        if (morning && string.Compare(d.MorningEnd, d.MorningStart, StringComparison.Ordinal) <= 0)
            yield return "Eindtijd moet na starttijd zijn";
        if (afternoon && string.Compare(d.AfternoonEnd, d.AfternoonStart, StringComparison.Ordinal) <= 0)
            yield return "Eindtijd moet na starttijd zijn";
        if (morning && afternoon && string.Compare(d.AfternoonStart, d.MorningEnd, StringComparison.Ordinal) < 0)
            yield return "De namiddag moet na de voormiddag beginnen";

        string first = morning ? d.MorningStart! : d.AfternoonStart!;
        string last = afternoon ? d.AfternoonEnd! : d.MorningEnd!;
        bool trainersInRange = d.Trainers.All(t =>
            string.Compare(t.StartTime, first, StringComparison.Ordinal) >= 0
            && string.Compare(t.EndTime, last, StringComparison.Ordinal) <= 0
            && string.Compare(t.EndTime, t.StartTime, StringComparison.Ordinal) > 0);
        if (!trainersInRange)
            yield return "Trainer-uren moeten binnen de kampuren van die dag vallen";

        if (d.Trainers.All(t => IsValidTime(t.StartTime) && IsValidTime(t.EndTime))
            && CampShiftRules.HasOverlappingShifts(d.Trainers))
            yield return CampShiftRules.OverlapMessage;
    }
}
