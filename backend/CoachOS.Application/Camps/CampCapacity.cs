using System.Globalization;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Camps;

/// <summary>Capaciteit per dagdeel: maximum = <see cref="Camp.MaxParticipants"/>, elke dag apart geteld.</summary>
internal static class CampCapacity
{
    public static bool IsFull(
        Camp camp, Dictionary<(Guid CampDayId, CampDayPart Part), int> perPart, Guid dayId, CampDayPart part)
        => camp.MaxParticipants.HasValue && perPart.GetValueOrDefault((dayId, part)) >= camp.MaxParticipants.Value;

    /// <summary>Eerste dagdeel in de keuze waar <paramref name="extra"/> nieuwe deelnemers niet meer bij passen, of null.</summary>
    public static string? FirstFullPart(
        Camp camp, Dictionary<(Guid CampDayId, CampDayPart Part), int> perPart,
        IEnumerable<CampDaySelectionDto> selection, int extra)
    {
        if (!camp.MaxParticipants.HasValue) return null;
        foreach (CampDaySelectionDto choice in selection)
        {
            string date = camp.Days.First(d => d.Id == choice.CampDayId).Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            if (choice.Morning && perPart.GetValueOrDefault((choice.CampDayId, CampDayPart.Morning)) + extra > camp.MaxParticipants.Value)
                return $"De voormiddag van {date} is volzet.";
            if (choice.Afternoon && perPart.GetValueOrDefault((choice.CampDayId, CampDayPart.Afternoon)) + extra > camp.MaxParticipants.Value)
                return $"De namiddag van {date} is volzet.";
        }
        return null;
    }

    /// <summary>Telling zonder de huidige dagdelen van één inschrijving (bij bewerken telt die niet dubbel).</summary>
    public static Dictionary<(Guid CampDayId, CampDayPart Part), int> WithoutOwnParts(
        Dictionary<(Guid CampDayId, CampDayPart Part), int> perPart, IEnumerable<CampEnrollmentDay> own)
    {
        Dictionary<(Guid CampDayId, CampDayPart Part), int> result = new(perPart);
        foreach (CampEnrollmentDay day in own)
        {
            if (day.Morning && result.TryGetValue((day.CampDayId, CampDayPart.Morning), out int m))
                result[(day.CampDayId, CampDayPart.Morning)] = m - 1;
            if (day.Afternoon && result.TryGetValue((day.CampDayId, CampDayPart.Afternoon), out int a))
                result[(day.CampDayId, CampDayPart.Afternoon)] = a - 1;
        }
        return result;
    }

    public static List<CampEnrollmentDay> BuildEnrollmentDays(Guid organizationId, IEnumerable<CampDaySelectionDto> selection)
        => selection.Select(c => new CampEnrollmentDay
        {
            OrganizationId = organizationId, CampDayId = c.CampDayId, Morning = c.Morning, Afternoon = c.Afternoon,
        }).ToList();

    public static List<CampDaySelectionDto> ToSelection(IEnumerable<CampEnrollmentDay> days)
        => days.Select(d => new CampDaySelectionDto(d.CampDayId, d.Morning, d.Afternoon)).ToList();
}
