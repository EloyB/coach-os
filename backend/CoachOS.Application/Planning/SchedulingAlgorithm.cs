using CoachOS.Domain.Enums;

namespace CoachOS.Application.Planning;

/// <summary>
/// Pure scheduling algorithm. No dependencies, no side effects.
/// Input: enrollments with preferences and slot capacities.
/// Output: proposed assignments and conflicts.
/// </summary>
public static class SchedulingAlgorithm
{
    public record SchedulingInput(
        List<EnrollmentUnit> Units,
        List<SlotInfo> Slots);

    public record EnrollmentUnit(
        Guid Id,
        bool IsGroup,
        Guid? GroupId,
        List<Guid> EnrollmentIds,
        List<string> StudentNames,
        int Size,
        bool IsOpenToGrouping,
        Dictionary<Guid, SlotPreference> Preferences);

    public record SlotInfo(
        Guid WeeklyTemplateEntryId,
        int MaxCapacity);

    public record SchedulingResult(
        List<ProposedAssignment> Assignments,
        List<ConflictItem> Conflicts);

    public record ProposedAssignment(
        Guid WeeklyTemplateEntryId,
        Guid? GroupId,
        Guid? EnrollmentId,
        int Size);

    public record ConflictItem(
        Guid EnrollmentId,
        string StudentName,
        string Reason);

    public static SchedulingResult Generate(SchedulingInput input)
    {
        var assignments = new List<ProposedAssignment>();
        var conflicts = new List<ConflictItem>();
        var slotCapacity = input.Slots.ToDictionary(s => s.WeeklyTemplateEntryId, s => s.MaxCapacity);
        var slotUsed = input.Slots.ToDictionary(s => s.WeeklyTemplateEntryId, _ => 0);
        var assigned = new HashSet<Guid>(); // unit IDs that have been assigned

        var groups = input.Units.Where(u => u.IsGroup).ToList();
        var solos = input.Units.Where(u => !u.IsGroup).ToList();

        // Step 1: Lock pre-formed groups to their best viable slot
        foreach (var group in groups)
        {
            var bestSlot = GetBestViableSlot(group, slotCapacity, slotUsed);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, group.GroupId, null, group.Size));
                slotUsed[bestSlot.Value] += group.Size;
                assigned.Add(group.Id);
            }
        }

        // Step 2: Lock uncontested slots (iterate until stable)
        var unassignedSolos = solos.Where(u => !assigned.Contains(u.Id)).ToList();
        bool changed;
        do
        {
            changed = false;
            foreach (var slotId in slotCapacity.Keys)
            {
                var remaining = slotCapacity[slotId] - slotUsed[slotId];
                if (remaining <= 0) continue;

                var candidates = unassignedSolos
                    .Where(u => !assigned.Contains(u.Id)
                        && u.Preferences.TryGetValue(slotId, out var pref)
                        && pref != SlotPreference.Unavailable)
                    .ToList();

                if (candidates.Count == 1 && candidates[0].Size <= remaining)
                {
                    var unit = candidates[0];
                    assignments.Add(new ProposedAssignment(slotId, null, unit.EnrollmentIds[0], unit.Size));
                    slotUsed[slotId] += unit.Size;
                    assigned.Add(unit.Id);
                    changed = true;
                }
            }
        } while (changed);

        // Step 3: Auto-group open solos with overlapping preferred slots
        var openSolos = unassignedSolos
            .Where(u => !assigned.Contains(u.Id) && u.IsOpenToGrouping)
            .ToList();

        if (openSolos.Count >= 2)
        {
            var autoGrouped = AutoGroupSolos(openSolos, slotCapacity, slotUsed);
            foreach (var (slotId, members) in autoGrouped)
            {
                foreach (var member in members)
                {
                    assignments.Add(new ProposedAssignment(slotId, null, member.EnrollmentIds[0], member.Size));
                    slotUsed[slotId] += member.Size;
                    assigned.Add(member.Id);
                }
            }
        }

        // Step 4: Assign remaining solos to best available slot
        var remaining2 = unassignedSolos.Where(u => !assigned.Contains(u.Id)).ToList();
        foreach (var unit in remaining2)
        {
            var bestSlot = GetBestViableSlot(unit, slotCapacity, slotUsed);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, null, unit.EnrollmentIds[0], unit.Size));
                slotUsed[bestSlot.Value] += unit.Size;
                assigned.Add(unit.Id);
            }
        }

        // Step 5: Flag conflicts for unassigned units
        foreach (var unit in input.Units.Where(u => !assigned.Contains(u.Id)))
        {
            foreach (var name in unit.StudentNames)
            {
                conflicts.Add(new ConflictItem(
                    unit.EnrollmentIds[0],
                    name,
                    "Geen beschikbaar slot met voldoende capaciteit"));
            }
        }

        return new SchedulingResult(assignments, conflicts);
    }

    private static Guid? GetBestViableSlot(
        EnrollmentUnit unit,
        Dictionary<Guid, int> slotCapacity,
        Dictionary<Guid, int> slotUsed)
    {
        return unit.Preferences
            .Where(p => p.Value != SlotPreference.Unavailable
                && slotCapacity.ContainsKey(p.Key)
                && slotCapacity[p.Key] - slotUsed[p.Key] >= unit.Size)
            .OrderBy(p => p.Value) // Preferred (2) before Available (1) — lower enum wins
            .ThenBy(p => slotUsed[p.Key]) // prefer emptier slots
            .Select(p => (Guid?)p.Key)
            .FirstOrDefault();
    }

    private static List<(Guid SlotId, List<EnrollmentUnit> Members)> AutoGroupSolos(
        List<EnrollmentUnit> openSolos,
        Dictionary<Guid, int> slotCapacity,
        Dictionary<Guid, int> slotUsed)
    {
        var result = new List<(Guid, List<EnrollmentUnit>)>();
        var used = new HashSet<Guid>();

        // For each slot, find solos that prefer it and group them
        foreach (var slotId in slotCapacity.Keys)
        {
            var remaining = slotCapacity[slotId] - slotUsed[slotId];
            if (remaining < 2) continue;

            var candidates = openSolos
                .Where(u => !used.Contains(u.Id)
                    && u.Preferences.TryGetValue(slotId, out var pref)
                    && pref == SlotPreference.Preferred)
                .Take(remaining)
                .ToList();

            if (candidates.Count >= 2)
            {
                result.Add((slotId, candidates));
                foreach (var c in candidates)
                    used.Add(c.Id);
            }
        }

        return result;
    }
}
