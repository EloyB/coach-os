using AwesomeAssertions;
using CoachOS.Application.Planning;
using CoachOS.Domain.Enums;
using NUnit.Framework;
using static CoachOS.Application.Planning.SchedulingAlgorithm;

namespace CoachOS.Tests.Services;

/// <summary>
/// Het maximum aantal personen per tijdslot (#387): de automatische planning plaatst niemand in een slot waar
/// uiteindelijk meer personen zitten dan zijn of haar maximum, ook niet bij samenvoegen.
/// </summary>
[TestFixture]
public class SchedulingMaxGroupSizeTests
{
    private static readonly Guid Slot1 = Guid.NewGuid();
    private static readonly Guid Slot2 = Guid.NewGuid();

    private static Dictionary<Guid, SlotPreference> Prefer(params Guid[] slots)
        => slots.ToDictionary(s => s, _ => SlotPreference.Preferred);

    private static EnrollmentUnit Solo(string name, int maxGroupSize, params Guid[] preferredSlots)
    {
        Guid id = Guid.NewGuid();
        return new EnrollmentUnit(id, false, null, [id], [name], 1, maxGroupSize, Prefer(preferredSlots));
    }

    private static EnrollmentUnit Group(string name, int size, int maxGroupSize, params Guid[] preferredSlots)
    {
        Guid groupId = Guid.NewGuid();
        List<Guid> ids = Enumerable.Range(0, size).Select(_ => Guid.NewGuid()).ToList();
        return new EnrollmentUnit(
            groupId, true, groupId, ids, ids.Select((_, i) => $"{name}_{i}").ToList(), size, maxGroupSize, Prefer(preferredSlots));
    }

    private static Dictionary<Guid, int> PeoplePerSlot(SchedulingResult result, IEnumerable<EnrollmentUnit> units)
    {
        Dictionary<Guid, EnrollmentUnit> byKey = new();
        foreach (EnrollmentUnit u in units)
        {
            byKey[u.IsGroup ? u.GroupId!.Value : u.EnrollmentIds[0]] = u;
        }

        return result.Assignments
            .GroupBy(a => a.WeeklyTemplateEntryId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Size));
    }

    [Test]
    public void GroupOfTwoWithMaxThree_CanBeJoinedByExactlyOnePerson()
    {
        EnrollmentUnit group = Group("Duo", 2, maxGroupSize: 3, Slot1);
        EnrollmentUnit anna = Solo("Anna", 3, Slot1);
        EnrollmentUnit bram = Solo("Bram", 3, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([group, anna, bram], [new SlotInfo(Slot1, 4)]));

        PeoplePerSlot(result, [group, anna, bram])[Slot1].Should().Be(3);
        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().HaveCount(1);
    }

    [Test]
    public void Merging_StaysWithinTheSmallestMaximumOfEveryoneInTheSlot()
    {
        // Anna wil maximum met twee, Bram en Carl accepteren vier. Samen in één slot mag dus maar met twee.
        EnrollmentUnit anna = Solo("Anna", 2, Slot1);
        EnrollmentUnit bram = Solo("Bram", 4, Slot1);
        EnrollmentUnit carl = Solo("Carl", 4, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([anna, bram, carl], [new SlotInfo(Slot1, 4)]));

        PeoplePerSlot(result, [anna, bram, carl])[Slot1].Should().BeLessThanOrEqualTo(2);
        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().HaveCount(1);
    }

    [Test]
    public void Merging_UsesAnotherSlotForTheOthersInsteadOfOverfillingOne()
    {
        EnrollmentUnit anna = Solo("Anna", 2, Slot1, Slot2);
        EnrollmentUnit bram = Solo("Bram", 2, Slot1, Slot2);
        EnrollmentUnit carl = Solo("Carl", 2, Slot1, Slot2);
        EnrollmentUnit dora = Solo("Dora", 2, Slot1, Slot2);

        SchedulingResult result = Generate(new SchedulingInput(
            [anna, bram, carl, dora], [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]));

        result.Conflicts.Should().BeEmpty();
        PeoplePerSlot(result, [anna, bram, carl, dora]).Values.Should().AllSatisfy(v => v.Should().BeLessThanOrEqualTo(2));
    }

    [Test]
    public void SoloWithMaxOne_NeverJoinsAnOccupiedSlot_EvenAsLastResort()
    {
        EnrollmentUnit open1 = Solo("Open1", 4, Slot1);
        EnrollmentUnit open2 = Solo("Open2", 4, Slot1);
        EnrollmentUnit solo = Solo("Solo", 1, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([open1, open2, solo], [new SlotInfo(Slot1, 4)]));

        // Solo kan enkel een leeg slot krijgen; dat bestaat niet, dus een conflict. De twee anderen delen het slot.
        result.Conflicts.Select(c => c.StudentName).Should().Equal("Solo");
        result.Assignments.Should().HaveCount(2);
    }

    [Test]
    public void ClosedGroup_KeepsItsSlotToItself()
    {
        EnrollmentUnit closed = Group("Gesloten", 2, maxGroupSize: 2, Slot1);
        EnrollmentUnit anna = Solo("Anna", 4, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([closed, anna], [new SlotInfo(Slot1, 4)]));

        result.Assignments.Should().ContainSingle();
        result.Conflicts.Should().ContainSingle();
    }

    [Test]
    public void ExistingOccupants_LimitHowLargeTheSlotMayBecome()
    {
        // Eén persoon ligt al vast (met maximum 2), er zijn nog drie plaatsen vrij.
        EnrollmentUnit anna = Solo("Anna", 4, Slot1);
        EnrollmentUnit bram = Solo("Bram", 4, Slot1);

        SchedulingResult result = Generate(new SchedulingInput(
            [anna, bram],
            [new SlotInfo(Slot1, MaxCapacity: 3, OccupantCount: 1, OccupantsMaxGroupSize: 2)]));

        result.Assignments.Should().ContainSingle();
        result.Conflicts.Should().ContainSingle();
    }

    [Test]
    public void ExistingOccupants_WithRoomLeft_AllowTheSlotToGrow()
    {
        EnrollmentUnit anna = Solo("Anna", 4, Slot1);
        EnrollmentUnit bram = Solo("Bram", 4, Slot1);

        SchedulingResult result = Generate(new SchedulingInput(
            [anna, bram],
            [new SlotInfo(Slot1, MaxCapacity: 3, OccupantCount: 1, OccupantsMaxGroupSize: 3)]));

        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void MaxSmallerThanOwnGroup_IsTreatedAsTheGroupSize()
    {
        // Onverwachte data (maximum 1 voor een groep van drie) mag de planning niet blokkeren.
        EnrollmentUnit group = Group("Familie", 3, maxGroupSize: 1, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([group], [new SlotInfo(Slot1, 4)]));

        result.Assignments.Should().ContainSingle();
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Conflict_ExplainsThatTheMaximumCanBeTheReason()
    {
        EnrollmentUnit closed = Group("Gesloten", 2, maxGroupSize: 2, Slot1);
        EnrollmentUnit anna = Solo("Anna", 4, Slot1);

        SchedulingResult result = Generate(new SchedulingInput([closed, anna], [new SlotInfo(Slot1, 4)]));

        result.Conflicts.Single().Reason.Should().Contain("maximum");
    }

    /// <summary>
    /// Invariant over willekeurige invoer: in geen enkel slot zitten meer personen dan de capaciteit of dan het
    /// maximum van iemand in dat slot (rekening houdend met wie er al vastligt).
    /// </summary>
    [Test]
    public void RandomInput_NeverPutsAnyoneInASlotLargerThanTheirMaximum()
    {
        Random random = new(387);
        for (int round = 0; round < 500; round++)
        {
            Guid[] slotIds = Enumerable.Range(0, random.Next(1, 4)).Select(_ => Guid.NewGuid()).ToArray();
            List<SlotInfo> slots = slotIds.Select(id =>
            {
                int capacity = random.Next(1, 5);
                // Soms ligt er al iemand vast.
                int occupants = random.Next(0, 3) == 0 ? random.Next(1, capacity + 1) : 0;
                int ceiling = occupants > 0 ? random.Next(occupants, 5) : int.MaxValue;
                return new SlotInfo(id, capacity - occupants, occupants, ceiling);
            }).ToList();

            List<EnrollmentUnit> units = [];
            int unitCount = random.Next(1, 9);
            for (int i = 0; i < unitCount; i++)
            {
                int size = random.Next(1, 4) == 1 ? random.Next(2, 4) : 1;
                int max = random.Next(size, 5);
                Dictionary<Guid, SlotPreference> prefs = slotIds
                    .Where(_ => random.Next(0, 3) > 0)
                    .ToDictionary(s => s, _ => (SlotPreference)random.Next(1, 4));
                Guid id = Guid.NewGuid();
                units.Add(size == 1
                    ? new EnrollmentUnit(id, false, null, [id], [$"S{i}"], 1, max, prefs)
                    : new EnrollmentUnit(id, true, id, Enumerable.Range(0, size).Select(_ => Guid.NewGuid()).ToList(),
                        Enumerable.Range(0, size).Select(k => $"G{i}_{k}").ToList(), size, max, prefs));
            }

            SchedulingResult result = Generate(new SchedulingInput(units, slots));

            foreach (IGrouping<Guid, ProposedAssignment> inSlot in result.Assignments.GroupBy(a => a.WeeklyTemplateEntryId))
            {
                SlotInfo slot = slots.Single(s => s.WeeklyTemplateEntryId == inSlot.Key);
                int added = inSlot.Sum(a => a.Size);
                int total = slot.OccupantCount + added;

                added.Should().BeLessThanOrEqualTo(slot.MaxCapacity, $"ronde {round}: capaciteit");
                total.Should().BeLessThanOrEqualTo(slot.OccupantsMaxGroupSize, $"ronde {round}: maximum van wie er al zat");

                foreach (ProposedAssignment a in inSlot)
                {
                    EnrollmentUnit unit = units.Single(u => u.IsGroup
                        ? u.GroupId == a.GroupId
                        : u.EnrollmentIds[0] == a.EnrollmentId);
                    total.Should().BeLessThanOrEqualTo(unit.EffectiveMaxGroupSize,
                        $"ronde {round}: {unit.StudentNames[0]} zit in een slot met {total} personen, maximum {unit.EffectiveMaxGroupSize}");
                }
            }

            // Niemand wordt twee keer ingepland.
            result.Assignments.Select(a => a.GroupId?.ToString() ?? a.EnrollmentId!.Value.ToString())
                .Should().OnlyHaveUniqueItems($"ronde {round}");
        }
    }
}
