using CoachOS.Domain.Enums;

namespace CoachOS.Application.Planning;

/// <summary>
/// Pure scheduling algorithm. No dependencies, no side effects.
/// Input: enrollments with preferences and slot capacities.
/// Output: proposed assignments and conflicts.
/// </summary>
public static class SchedulingAlgorithm
{
    public static SchedulingResult Generate(SchedulingInput input)
    {
        List<ProposedAssignment> assignments = new();
        List<ConflictItem> conflicts = new();
        Dictionary<Guid, int> slotCapacity = input.Slots.ToDictionary(s => s.WeeklyTemplateEntryId, s => s.MaxCapacity);
        Dictionary<Guid, int> slotUsed = input.Slots.ToDictionary(s => s.WeeklyTemplateEntryId, _ => 0);
        // Track which slots have assignments and whether they are open to merging
        Dictionary<Guid, bool> slotIsOpenToMerging = new();
        // Track the age category a slot is locked to. A slot becomes locked the first time an
        // age-bearing unit is placed in it; afterwards only units of the same bucket (or units
        // with no age category) may be auto-grouped into it. Slots absent from this map, or
        // mapped to null, are unconstrained.
        Dictionary<Guid, string?> slotAgeCategory = new();
        HashSet<Guid> assigned = new(); // unit IDs that have been assigned

        List<EnrollmentUnit> groups = input.Units.Where(u => u.IsGroup).ToList();
        List<EnrollmentUnit> solos = input.Units.Where(u => !u.IsGroup).ToList();

        // Step 1: Lock pre-formed groups that are NOT open to grouping (exclusive slots)
        foreach (EnrollmentUnit group in groups.Where(g => !g.IsOpenToGrouping))
        {
            Guid? bestSlot = GetBestExclusiveSlot(group, slotCapacity, slotUsed, slotIsOpenToMerging);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, group.GroupId, null, group.Size));
                slotUsed[bestSlot.Value] += group.Size;
                slotIsOpenToMerging[bestSlot.Value] = false;
                LockSlotAge(slotAgeCategory, bestSlot.Value, group.AgeCategory);
                assigned.Add(group.Id);
            }
        }

        // Step 2: Lock pre-formed groups that ARE open to grouping
        foreach (EnrollmentUnit group in groups.Where(g => g.IsOpenToGrouping && !assigned.Contains(g.Id)))
        {
            Guid? bestSlot = GetBestViableSlot(group, slotCapacity, slotUsed, slotAgeCategory);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, group.GroupId, null, group.Size));
                slotUsed[bestSlot.Value] += group.Size;
                slotIsOpenToMerging[bestSlot.Value] = true;
                LockSlotAge(slotAgeCategory, bestSlot.Value, group.AgeCategory);
                assigned.Add(group.Id);
            }
        }

        // Step 3: Lock uncontested slots (iterate until stable)
        List<EnrollmentUnit> unassignedSolos = solos.Where(u => !assigned.Contains(u.Id)).ToList();
        bool changed;
        do
        {
            changed = false;
            foreach (Guid slotId in slotCapacity.Keys)
            {
                int remaining = slotCapacity[slotId] - slotUsed[slotId];
                if (remaining <= 0) continue;

                // Skip slots that are exclusively taken (not open to merging)
                if (slotIsOpenToMerging.TryGetValue(slotId, out bool isOpen) && !isOpen)
                    continue;

                List<EnrollmentUnit> candidates = unassignedSolos
                    .Where(u => !assigned.Contains(u.Id)
                        && u.Preferences.TryGetValue(slotId, out SlotPreference pref)
                        && pref != SlotPreference.Unavailable
                        && CanShareSlot(slotAgeCategory, slotId, u.AgeCategory))
                    .ToList();

                if (candidates.Count == 1 && candidates[0].Size <= remaining)
                {
                    EnrollmentUnit unit = candidates[0];
                    assignments.Add(new ProposedAssignment(slotId, null, unit.EnrollmentIds[0], unit.Size));
                    slotUsed[slotId] += unit.Size;
                    LockSlotAge(slotAgeCategory, slotId, unit.AgeCategory);
                    assigned.Add(unit.Id);
                    changed = true;
                }
            }
        } while (changed);

        // Step 4: Auto-merge open units (solos AND groups with IsOpenToGrouping)
        List<EnrollmentUnit> openUnits = input.Units
            .Where(u => !assigned.Contains(u.Id) && u.IsOpenToGrouping)
            .ToList();

        if (openUnits.Count >= 2)
        {
            List<(Guid SlotId, List<EnrollmentUnit> Members)> autoMerged = AutoMergeUnits(openUnits, slotCapacity, slotUsed, slotIsOpenToMerging, slotAgeCategory);
            foreach ((Guid slotId, List<EnrollmentUnit> members) in autoMerged)
            {
                foreach (EnrollmentUnit member in members)
                {
                    if (member.IsGroup)
                    {
                        assignments.Add(new ProposedAssignment(slotId, member.GroupId, null, member.Size, IsAutoMerged: true));
                    }
                    else
                    {
                        assignments.Add(new ProposedAssignment(slotId, null, member.EnrollmentIds[0], member.Size, IsAutoMerged: true));
                    }
                    slotUsed[slotId] += member.Size;
                    assigned.Add(member.Id);
                }
            }
        }

        // Step 5: Assign remaining solos to best available slot
        List<EnrollmentUnit> remainingSolos = unassignedSolos.Where(u => !assigned.Contains(u.Id)).ToList();
        foreach (EnrollmentUnit unit in remainingSolos)
        {
            Guid? bestSlot = GetBestViableSlot(unit, slotCapacity, slotUsed, slotAgeCategory);
            if (bestSlot.HasValue)
            {
                // Check if this slot already has assignments that are open to merging
                bool isMerging = slotIsOpenToMerging.TryGetValue(bestSlot.Value, out bool open) && open && slotUsed[bestSlot.Value] > 0;
                assignments.Add(new ProposedAssignment(bestSlot.Value, null, unit.EnrollmentIds[0], unit.Size, IsAutoMerged: isMerging));
                slotUsed[bestSlot.Value] += unit.Size;
                LockSlotAge(slotAgeCategory, bestSlot.Value, unit.AgeCategory);
                assigned.Add(unit.Id);
            }
        }

        // Step 6: Flag conflicts for unassigned units
        foreach (EnrollmentUnit unit in input.Units.Where(u => !assigned.Contains(u.Id)))
        {
            foreach (string name in unit.StudentNames)
            {
                conflicts.Add(new ConflictItem(
                    unit.EnrollmentIds[0],
                    name,
                    "Geen beschikbaar slot met voldoende capaciteit"));
            }
        }

        // Post-process: mark existing assignments in slots that got auto-merged units
        HashSet<Guid> autoMergedSlots = new(assignments.Where(a => a.IsAutoMerged).Select(a => a.WeeklyTemplateEntryId));
        assignments = assignments.Select(a =>
            autoMergedSlots.Contains(a.WeeklyTemplateEntryId) ? a with { IsAutoMerged = true } : a
        ).ToList();

        return new SchedulingResult(assignments, conflicts);
    }

    /// <summary>
    /// Find the best slot that is either empty or only has open-to-merging assignments.
    /// </summary>
    private static Guid? GetBestExclusiveSlot(
        EnrollmentUnit unit,
        Dictionary<Guid, int> slotCapacity,
        Dictionary<Guid, int> slotUsed,
        Dictionary<Guid, bool> slotIsOpenToMerging)
    {
        return unit.Preferences
            .Where(p => p.Value != SlotPreference.Unavailable
                && slotCapacity.ContainsKey(p.Key)
                && slotCapacity[p.Key] - slotUsed[p.Key] >= unit.Size
                && !slotIsOpenToMerging.ContainsKey(p.Key)) // only empty/unassigned slots
            .OrderByDescending(p => p.Value)
            .ThenBy(p => slotUsed[p.Key])
            .Select(p => (Guid?)p.Key)
            .FirstOrDefault();
    }

    private static Guid? GetBestViableSlot(
        EnrollmentUnit unit,
        Dictionary<Guid, int> slotCapacity,
        Dictionary<Guid, int> slotUsed,
        Dictionary<Guid, string?> slotAgeCategory)
    {
        return unit.Preferences
            .Where(p => p.Value != SlotPreference.Unavailable
                && slotCapacity.ContainsKey(p.Key)
                && slotCapacity[p.Key] - slotUsed[p.Key] >= unit.Size
                && CanShareSlot(slotAgeCategory, p.Key, unit.AgeCategory))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => slotUsed[p.Key])
            .Select(p => (Guid?)p.Key)
            .FirstOrDefault();
    }

    /// <summary>
    /// Whether a unit may join a slot given age-category locking. A unit with no age category is
    /// unconstrained; an unlocked slot (absent or null) accepts anyone; otherwise buckets must match.
    /// </summary>
    private static bool CanShareSlot(Dictionary<Guid, string?> slotAgeCategory, Guid slotId, string? unitAge)
        => unitAge is null
            || !slotAgeCategory.TryGetValue(slotId, out string? locked)
            || locked is null
            || locked == unitAge;

    /// <summary>
    /// Lock a slot to a unit's age bucket on first placement. No-op for units without an age
    /// category or slots already locked to a (non-null) bucket.
    /// </summary>
    private static void LockSlotAge(Dictionary<Guid, string?> slotAgeCategory, Guid slotId, string? unitAge)
    {
        if (unitAge is null) return;
        if (!slotAgeCategory.TryGetValue(slotId, out string? locked) || locked is null)
            slotAgeCategory[slotId] = unitAge;
    }

    /// <summary>
    /// Auto-merge open units (solos and groups with IsOpenToGrouping) into shared slots.
    /// </summary>
    private static List<(Guid SlotId, List<EnrollmentUnit> Members)> AutoMergeUnits(
        List<EnrollmentUnit> openUnits,
        Dictionary<Guid, int> slotCapacity,
        Dictionary<Guid, int> slotUsed,
        Dictionary<Guid, bool> slotIsOpenToMerging,
        Dictionary<Guid, string?> slotAgeCategory)
    {
        List<(Guid, List<EnrollmentUnit>)> result = new();
        HashSet<Guid> used = new();

        foreach (Guid slotId in slotCapacity.Keys)
        {
            // Skip slots that are exclusively taken
            if (slotIsOpenToMerging.TryGetValue(slotId, out bool isOpen) && !isOpen)
                continue;

            int remaining = slotCapacity[slotId] - slotUsed[slotId];
            if (remaining < 2) continue;

            List<EnrollmentUnit> candidates = openUnits
                .Where(u => !used.Contains(u.Id)
                    && u.Size <= remaining
                    && u.Preferences.TryGetValue(slotId, out SlotPreference pref)
                    && pref == SlotPreference.Preferred
                    && CanShareSlot(slotAgeCategory, slotId, u.AgeCategory))
                .ToList();

            // A slot can only hold one age bucket. If already locked, keep that bucket; otherwise
            // pick the most-represented bucket among candidates so we merge as many as possible.
            // Units without an age category are compatible with whatever bucket the slot settles on.
            string? targetBucket = slotAgeCategory.GetValueOrDefault(slotId);
            targetBucket ??= candidates
                .Where(c => c.AgeCategory is not null)
                .GroupBy(c => c.AgeCategory)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            List<EnrollmentUnit> eligible = targetBucket is null
                ? candidates
                : candidates.Where(c => c.AgeCategory == targetBucket || c.AgeCategory is null).ToList();

            // Greedily pack eligible candidates into the slot
            List<EnrollmentUnit> selected = new();
            int filled = 0;
            foreach (EnrollmentUnit candidate in eligible)
            {
                if (filled + candidate.Size <= remaining)
                {
                    selected.Add(candidate);
                    filled += candidate.Size;
                }
            }

            if (selected.Count >= 2 || (selected.Count >= 1 && slotUsed[slotId] > 0))
            {
                result.Add((slotId, selected));
                LockSlotAge(slotAgeCategory, slotId, targetBucket);
                foreach (EnrollmentUnit c in selected)
                    used.Add(c.Id);
            }
        }

        return result;
    }
}
