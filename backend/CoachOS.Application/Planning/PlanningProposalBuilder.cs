using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Planning;

/// <summary>
/// Pure-function helpers for assembling a schedule proposal:
/// preference lookup tables, group/solo enrollment units, and conflict detection.
/// Kept separate from PlanningService so the service stays focused on orchestration.
/// </summary>
internal static class PlanningProposalBuilder
{
    public static Dictionary<Guid, Dictionary<Guid, SlotPreference>> BuildPreferencesLookup(
        IEnumerable<TimeSlotPreference> preferences)
        => preferences
            .GroupBy(p => p.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(p => p.WeeklyTemplateEntryId, p => p.Preference));

    public static (
        List<EnrollmentUnit> units,
        HashSet<Guid> groupedEnrollmentIds)
    BuildUnits(
        IReadOnlyList<Enrollment> activeEnrollments,
        IReadOnlyList<EnrollmentGroup> groups,
        Dictionary<Guid, Dictionary<Guid, SlotPreference>> prefsByEnrollment,
        HashSet<Guid> lockedGroupIds,
        HashSet<Guid> lockedEnrollmentIds)
    {
        var units = new List<EnrollmentUnit>();
        var groupedEnrollmentIds = new HashSet<Guid>();

        foreach (var group in groups)
        {
            var memberEnrollments = activeEnrollments
                .Where(e => e.EnrollmentGroupId == group.Id)
                .ToList();

            if (memberEnrollments.Count == 0) continue;

            // Skip locked groups: they stay where the admin put them.
            if (lockedGroupIds.Contains(group.Id))
            {
                foreach (var e in memberEnrollments)
                    groupedEnrollmentIds.Add(e.Id);
                continue;
            }

            var membersWithPrefs = memberEnrollments
                .Where(e => prefsByEnrollment.ContainsKey(e.Id))
                .ToList();

            Dictionary<Guid, SlotPreference> groupPrefs = membersWithPrefs.Count switch
            {
                0 => new(),
                1 => prefsByEnrollment[membersWithPrefs[0].Id],
                _ => IntersectPreferences(membersWithPrefs.Select(e => prefsByEnrollment[e.Id]).ToList()),
            };

            var leaderEnrollment = memberEnrollments.FirstOrDefault(e => e.Id == group.LeaderEnrollmentId);
            units.Add(new EnrollmentUnit(
                Id: group.Id,
                IsGroup: true,
                GroupId: group.Id,
                EnrollmentIds: memberEnrollments.Select(e => e.Id).ToList(),
                StudentNames: memberEnrollments.Select(e => e.StudentName).ToList(),
                Size: memberEnrollments.Count,
                IsOpenToGrouping: leaderEnrollment?.IsOpenToGrouping ?? false,
                Preferences: groupPrefs,
                AgeCategory: GetSharedAgeCategory(memberEnrollments)));

            foreach (var e in memberEnrollments)
                groupedEnrollmentIds.Add(e.Id);
        }

        foreach (var enrollment in activeEnrollments.Where(e =>
            !groupedEnrollmentIds.Contains(e.Id) && !lockedEnrollmentIds.Contains(e.Id)))
        {
            var prefs = prefsByEnrollment.GetValueOrDefault(enrollment.Id, new());
            units.Add(new EnrollmentUnit(
                Id: enrollment.Id,
                IsGroup: false,
                GroupId: null,
                EnrollmentIds: [enrollment.Id],
                StudentNames: [enrollment.StudentName],
                Size: 1,
                IsOpenToGrouping: enrollment.IsOpenToGrouping,
                Preferences: prefs,
                AgeCategory: GetAgeCategory(enrollment)));
        }

        return (units, groupedEnrollmentIds);
    }

    public static List<PlanningConflictDto> BuildConflicts(
        IReadOnlyList<Enrollment> activeEnrollments,
        IReadOnlyList<ScheduleAssignment> assignments,
        IReadOnlyList<WeeklyTemplateEntry> slots,
        Dictionary<Guid, Dictionary<Guid, SlotPreference>> prefsByEnrollment)
    {
        var conflicts = new List<PlanningConflictDto>();

        var assignedEnrollmentIds = assignments
            .SelectMany(a => a.EnrollmentGroup is not null
                ? a.EnrollmentGroup.Members.Select(m => m.Id)
                : a.EnrollmentId.HasValue ? [a.EnrollmentId.Value] : Array.Empty<Guid>())
            .ToHashSet();

        // 1. No viable slot — unassigned + every slot is Unavailable (or missing from prefs)
        foreach (var enrollment in activeEnrollments)
        {
            if (assignedEnrollmentIds.Contains(enrollment.Id)) continue;

            var prefs = prefsByEnrollment.GetValueOrDefault(enrollment.Id, new());
            bool hasViable = slots.Any(s =>
                prefs.TryGetValue(s.Id, out var p) &&
                (p == SlotPreference.Preferred || p == SlotPreference.Available));

            if (!hasViable)
                conflicts.Add(new PlanningConflictDto
                {
                    EnrollmentId = enrollment.Id,
                    Type = "no_viable_slot",
                    Message = $"{enrollment.StudentName} heeft geen beschikbaar tijdslot.",
                });
        }

        // 2. Oversubscribed slot — assigned count exceeds MaxStudents
        foreach (var slot in slots)
        {
            var count = assignments
                .Where(a => a.WeeklyTemplateEntryId == slot.Id)
                .Sum(a => a.EnrollmentGroup?.Members.Count ?? 1);

            if (count > slot.MaxStudents)
                conflicts.Add(new PlanningConflictDto
                {
                    TimeSlotId = slot.Id,
                    Type = "oversubscribed",
                    Message = $"Tijdslot overboekt ({count}/{slot.MaxStudents}).",
                });
        }

        return conflicts;
    }

    /// <summary>
    /// The age-category bucket a student chose on the enrollment form, or null when the form
    /// has no age-category field (or it was left unanswered). The matching algorithm treats
    /// null as unconstrained.
    /// </summary>
    private static string? GetAgeCategory(Enrollment enrollment)
        => enrollment.FormResponses
            .FirstOrDefault(r => r.FormField?.Type == FormFieldType.AgeCategory)
            ?.Value;

    /// <summary>
    /// The shared age bucket of a pre-formed group: the single bucket if every member agrees,
    /// otherwise null. A mixed-age group is the user's explicit choice, so it stays unconstrained
    /// rather than blocking the group.
    /// </summary>
    private static string? GetSharedAgeCategory(List<Enrollment> members)
    {
        var buckets = members
            .Select(GetAgeCategory)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        return buckets.Count == 1 ? buckets[0] : null;
    }

    private static Dictionary<Guid, SlotPreference> IntersectPreferences(
        List<Dictionary<Guid, SlotPreference>> memberPrefs)
    {
        if (memberPrefs.Count == 0) return new();
        if (memberPrefs.Count == 1) return memberPrefs[0];

        var allSlotIds = memberPrefs.SelectMany(p => p.Keys).Distinct();
        var result = new Dictionary<Guid, SlotPreference>();

        foreach (var slotId in allSlotIds)
        {
            // Take worst preference among members (most restrictive).
            var worstPref = SlotPreference.Preferred;
            var allHave = true;

            foreach (var memberPref in memberPrefs)
            {
                if (!memberPref.TryGetValue(slotId, out var pref))
                {
                    allHave = false;
                    break;
                }

                if (pref == SlotPreference.Unavailable)
                {
                    worstPref = SlotPreference.Unavailable;
                    break;
                }

                if (pref == SlotPreference.Available && worstPref == SlotPreference.Preferred)
                    worstPref = SlotPreference.Available;
            }

            result[slotId] = allHave ? worstPref : SlotPreference.Unavailable;
        }

        return result;
    }
}
