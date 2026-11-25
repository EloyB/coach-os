using CoachOS.Domain.Enums;

namespace CoachOS.Application.Planning;

/// <summary>
/// Pure scheduling algorithm. No dependencies, no side effects.
/// Input: enrollments with preferences and slot capacities.
/// Output: proposed assignments and conflicts.
///
/// Elke eenheid (solo of groep) heeft een maximum groepsgrootte: niemand wordt in een slot geplaatst waar
/// uiteindelijk meer personen zitten dan zijn of haar maximum, ook niet bij samenvoegen. Het maximum van
/// wie er al vastligt (vergrendeld of bevestigd) telt mee.
/// </summary>
public static class SchedulingAlgorithm
{
    public static SchedulingResult Generate(SchedulingInput input)
    {
        List<ProposedAssignment> assignments = new();
        List<ConflictItem> conflicts = new();
        SlotLedger ledger = new(input.Slots);
        // Track which slots have assignments and whether they are open to merging
        Dictionary<Guid, bool> slotIsOpenToMerging = new();
        HashSet<Guid> assigned = new(); // unit IDs that have been assigned

        List<EnrollmentUnit> groups = input.Units.Where(u => u.IsGroup).ToList();
        List<EnrollmentUnit> solos = input.Units.Where(u => !u.IsGroup).ToList();

        // Step 1: Lock pre-formed groups that are NOT open to grouping (exclusive slots)
        foreach (EnrollmentUnit group in groups.Where(g => !g.IsOpenToGrouping))
        {
            Guid? bestSlot = GetBestExclusiveSlot(group, ledger, slotIsOpenToMerging);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, group.GroupId, null, group.Size));
                ledger.Place(group, bestSlot.Value);
                slotIsOpenToMerging[bestSlot.Value] = false;
                assigned.Add(group.Id);
            }
        }

        // Step 2: Lock pre-formed groups that ARE open to grouping
        foreach (EnrollmentUnit group in groups.Where(g => g.IsOpenToGrouping && !assigned.Contains(g.Id)))
        {
            Guid? bestSlot = GetBestViableSlot(group, ledger);
            if (bestSlot.HasValue)
            {
                assignments.Add(new ProposedAssignment(bestSlot.Value, group.GroupId, null, group.Size));
                ledger.Place(group, bestSlot.Value);
                slotIsOpenToMerging[bestSlot.Value] = true;
                assigned.Add(group.Id);
            }
        }

        // Step 3: Lock uncontested slots (iterate until stable)
        List<EnrollmentUnit> unassignedSolos = solos.Where(u => !assigned.Contains(u.Id)).ToList();
        bool changed;
        do
        {
            changed = false;
            foreach (Guid slotId in ledger.SlotIds)
            {
                if (ledger.Remaining(slotId) <= 0) continue;

                // Skip slots that are exclusively taken (not open to merging)
                if (slotIsOpenToMerging.TryGetValue(slotId, out bool isOpen) && !isOpen)
                    continue;

                List<EnrollmentUnit> candidates = unassignedSolos
                    .Where(u => !assigned.Contains(u.Id)
                        && u.Preferences.TryGetValue(slotId, out SlotPreference pref)
                        && pref != SlotPreference.Unavailable)
                    .ToList();

                if (candidates.Count == 1 && ledger.Fits(candidates[0], slotId))
                {
                    EnrollmentUnit unit = candidates[0];
                    assignments.Add(new ProposedAssignment(slotId, null, unit.EnrollmentIds[0], unit.Size));
                    ledger.Place(unit, slotId);
                    assigned.Add(unit.Id);
                    changed = true;
                }
            }
        } while (changed);

        // Step 4: Auto-merge open units (solos AND groups that accept a larger slot)
        List<EnrollmentUnit> openUnits = input.Units
            .Where(u => !assigned.Contains(u.Id) && u.IsOpenToGrouping)
            .ToList();

        if (openUnits.Count >= 2)
        {
            List<(Guid SlotId, List<EnrollmentUnit> Members)> autoMerged = AutoMergeUnits(openUnits, ledger, slotIsOpenToMerging);
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
                    ledger.Place(member, slotId);
                    assigned.Add(member.Id);
                }
            }
        }

        // Step 5: Assign remaining solos to best available slot
        List<EnrollmentUnit> remainingSolos = unassignedSolos.Where(u => !assigned.Contains(u.Id)).ToList();
        foreach (EnrollmentUnit unit in remainingSolos)
        {
            Guid? bestSlot = GetBestViableSlot(unit, ledger);
            if (bestSlot.HasValue)
            {
                // Check if this slot already has assignments that are open to merging
                bool isMerging = slotIsOpenToMerging.TryGetValue(bestSlot.Value, out bool open) && open && ledger.Used(bestSlot.Value) > 0;
                assignments.Add(new ProposedAssignment(bestSlot.Value, null, unit.EnrollmentIds[0], unit.Size, IsAutoMerged: isMerging));
                ledger.Place(unit, bestSlot.Value);
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
                    "Geen beschikbaar slot met voldoende capaciteit binnen het gekozen maximum aantal personen"));
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
    /// Houdt per slot bij hoeveel plaatsen er nog vrij zijn en tot welke groepsgrootte het slot nog mag groeien.
    /// </summary>
    private sealed class SlotLedger
    {
        private readonly Dictionary<Guid, int> _capacity;
        private readonly Dictionary<Guid, int> _occupants;
        private readonly Dictionary<Guid, int> _used;
        private readonly Dictionary<Guid, int> _ceiling;

        public SlotLedger(IEnumerable<SlotInfo> slots)
        {
            List<SlotInfo> list = slots.ToList();
            _capacity = list.ToDictionary(s => s.WeeklyTemplateEntryId, s => s.MaxCapacity);
            _occupants = list.ToDictionary(s => s.WeeklyTemplateEntryId, s => s.OccupantCount);
            _used = list.ToDictionary(s => s.WeeklyTemplateEntryId, _ => 0);
            _ceiling = list.ToDictionary(s => s.WeeklyTemplateEntryId, s => s.OccupantsMaxGroupSize);
        }

        public IEnumerable<Guid> SlotIds => _capacity.Keys;

        public bool Contains(Guid slotId) => _capacity.ContainsKey(slotId);

        /// <summary>Wat dit algoritme al in het slot gezet heeft (zonder wie er al vastlag).</summary>
        public int Used(Guid slotId) => _used[slotId];

        /// <summary>Aantal personen in het slot: wie er al vastligt plus wat er nu bij komt.</summary>
        public int Total(Guid slotId) => _occupants[slotId] + _used[slotId];

        /// <summary>Nog vrije plaatsen, rekening houdend met de capaciteit én het kleinste maximum in het slot.</summary>
        public int Remaining(Guid slotId)
            => Math.Min(_capacity[slotId] - _used[slotId], _ceiling[slotId] - Total(slotId));

        public int Ceiling(Guid slotId) => _ceiling[slotId];

        /// <summary>Past de eenheid erbij zonder de capaciteit of iemands maximum te overschrijden?</summary>
        public bool Fits(EnrollmentUnit unit, Guid slotId)
            => Contains(slotId)
               && _capacity[slotId] - _used[slotId] >= unit.Size
               && Total(slotId) + unit.Size <= Math.Min(_ceiling[slotId], unit.EffectiveMaxGroupSize);

        public void Place(EnrollmentUnit unit, Guid slotId)
        {
            _used[slotId] += unit.Size;
            _ceiling[slotId] = Math.Min(_ceiling[slotId], unit.EffectiveMaxGroupSize);
        }
    }

    /// <summary>
    /// Find the best slot that is either empty or only has open-to-merging assignments.
    /// </summary>
    private static Guid? GetBestExclusiveSlot(
        EnrollmentUnit unit,
        SlotLedger ledger,
        Dictionary<Guid, bool> slotIsOpenToMerging)
    {
        return unit.Preferences
            .Where(p => p.Value != SlotPreference.Unavailable
                && ledger.Fits(unit, p.Key)
                && !slotIsOpenToMerging.ContainsKey(p.Key)) // only empty/unassigned slots
            .OrderByDescending(p => p.Value)
            .ThenBy(p => ledger.Used(p.Key))
            .Select(p => (Guid?)p.Key)
            .FirstOrDefault();
    }

    private static Guid? GetBestViableSlot(EnrollmentUnit unit, SlotLedger ledger)
    {
        return unit.Preferences
            .Where(p => p.Value != SlotPreference.Unavailable && ledger.Fits(unit, p.Key))
            .OrderByDescending(p => p.Value)
            .ThenBy(p => ledger.Used(p.Key))
            .Select(p => (Guid?)p.Key)
            .FirstOrDefault();
    }

    /// <summary>
    /// Auto-merge open units (solos and groups that accept a larger slot) into shared slots. Een eenheid komt er
    /// enkel bij als het slot daarna niet groter is dan het kleinste maximum van iedereen die er dan zit.
    /// </summary>
    private static List<(Guid SlotId, List<EnrollmentUnit> Members)> AutoMergeUnits(
        List<EnrollmentUnit> openUnits,
        SlotLedger ledger,
        Dictionary<Guid, bool> slotIsOpenToMerging)
    {
        List<(Guid, List<EnrollmentUnit>)> result = new();
        HashSet<Guid> used = new();

        foreach (Guid slotId in ledger.SlotIds)
        {
            // Skip slots that are exclusively taken
            if (slotIsOpenToMerging.TryGetValue(slotId, out bool isOpen) && !isOpen)
                continue;

            int remaining = ledger.Remaining(slotId);
            if (remaining < 2) continue;

            List<EnrollmentUnit> candidates = openUnits
                .Where(u => !used.Contains(u.Id)
                    && u.Size <= remaining
                    && u.Preferences.TryGetValue(slotId, out SlotPreference pref)
                    && pref == SlotPreference.Preferred)
                .ToList();

            // Greedily pack candidates into the slot, binnen het maximum van iedereen die er dan zit.
            List<EnrollmentUnit> selected = new();
            int total = ledger.Total(slotId);
            int ceiling = ledger.Ceiling(slotId);
            int capacityLeft = remaining;
            foreach (EnrollmentUnit candidate in candidates)
            {
                int newTotal = total + candidate.Size;
                int newCeiling = Math.Min(ceiling, candidate.EffectiveMaxGroupSize);
                if (candidate.Size <= capacityLeft && newTotal <= newCeiling)
                {
                    selected.Add(candidate);
                    total = newTotal;
                    ceiling = newCeiling;
                    capacityLeft -= candidate.Size;
                }
            }

            if (selected.Count >= 2 || (selected.Count >= 1 && ledger.Used(slotId) > 0))
            {
                result.Add((slotId, selected));
                foreach (EnrollmentUnit c in selected)
                    used.Add(c.Id);
            }
        }

        return result;
    }
}
