using CoachOS.Application.Camps.DTOs;

namespace CoachOS.Application.Camps.Validators;

internal static class CampShiftRules
{
    public const string OverlapMessage = "Een trainer kan niet twee overlappende shiften op dezelfde dag hebben";

    /// <summary>
    /// True als dezelfde trainer op één dag twee shiften heeft die overlappen. Shiften die op elkaar
    /// aansluiten (einde = begin van de volgende) tellen niet als overlap. Tijden zijn HH:mm-strings.
    /// </summary>
    public static bool HasOverlappingShifts(IEnumerable<CreateCampDayTrainerRequest> trainers) =>
        trainers
            .GroupBy(t => t.TrainerId)
            .Any(group =>
            {
                List<CreateCampDayTrainerRequest> shifts = group
                    .OrderBy(s => s.StartTime, StringComparer.Ordinal)
                    .ToList();
                for (int i = 1; i < shifts.Count; i++)
                {
                    if (string.Compare(shifts[i].StartTime, shifts[i - 1].EndTime, StringComparison.Ordinal) < 0)
                        return true;
                }
                return false;
            });
}
