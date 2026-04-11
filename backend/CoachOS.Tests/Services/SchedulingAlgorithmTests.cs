using CoachOS.Application.Planning;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;
using static CoachOS.Application.Planning.SchedulingAlgorithm;

namespace CoachOS.Tests.Services;

[TestFixture]
public class SchedulingAlgorithmTests
{
    private static readonly Guid Slot1 = Guid.NewGuid();
    private static readonly Guid Slot2 = Guid.NewGuid();
    private static readonly Guid Slot3 = Guid.NewGuid();

    // ── Empty input ──────────────────────────────────────────────────────────

    [Test]
    public void Generate_EmptyInput_ReturnsEmptyResult()
    {
        var input = new SchedulingInput([], []);
        var result = Generate(input);

        result.Assignments.Should().BeEmpty();
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_NoEnrollments_ReturnsEmptyResult()
    {
        var input = new SchedulingInput(
            [],
            [new SlotInfo(Slot1, 4)]);

        var result = Generate(input);

        result.Assignments.Should().BeEmpty();
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_NoSlots_AllConflicts()
    {
        var enrollment = MakeSolo("Alice", new() { { Slot1, SlotPreference.Preferred } });
        var input = new SchedulingInput([enrollment], []);

        var result = Generate(input);

        result.Assignments.Should().BeEmpty();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].StudentName.Should().Be("Alice");
    }

    // ── Solo assignment ──────────────────────────────────────────────────────

    [Test]
    public void Generate_SingleSolo_AssignedToPreferredSlot()
    {
        // Two solos competing for both slots — algorithm should prefer Preferred over Available
        var alice = MakeSolo("Alice", new()
        {
            { Slot1, SlotPreference.Available },
            { Slot2, SlotPreference.Preferred },
        });
        var bob = MakeSolo("Bob", new()
        {
            { Slot1, SlotPreference.Preferred },
            { Slot2, SlotPreference.Available },
        });

        var input = new SchedulingInput(
            [alice, bob],
            [new SlotInfo(Slot1, 1), new SlotInfo(Slot2, 1)]); // capacity 1 each forces separation

        var result = Generate(input);

        result.Assignments.Should().HaveCount(2);
        // Alice prefers Slot2, Bob prefers Slot1 — each should get their preferred
        var aliceAssignment = result.Assignments.First(a => a.EnrollmentId == alice.EnrollmentIds[0]);
        var bobAssignment = result.Assignments.First(a => a.EnrollmentId == bob.EnrollmentIds[0]);
        aliceAssignment.WeeklyTemplateEntryId.Should().Be(Slot2);
        bobAssignment.WeeklyTemplateEntryId.Should().Be(Slot1);
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_SoloWithOnlyUnavailable_Conflict()
    {
        var enrollment = MakeSolo("Alice", new()
        {
            { Slot1, SlotPreference.Unavailable },
            { Slot2, SlotPreference.Unavailable },
        });

        var input = new SchedulingInput(
            [enrollment],
            [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]);

        var result = Generate(input);

        result.Assignments.Should().BeEmpty();
        result.Conflicts.Should().HaveCount(1);
    }

    [Test]
    public void Generate_SoloAssignedToAvailableWhenPreferredFull()
    {
        var solo1 = MakeSolo("Alice", new()
        {
            { Slot1, SlotPreference.Preferred },
            { Slot2, SlotPreference.Available },
        });

        // Fill Slot1 with a group of 4
        var group = MakeGroup("BigGroup", 4, new()
        {
            { Slot1, SlotPreference.Preferred },
        });

        var input = new SchedulingInput(
            [group, solo1],
            [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]);

        var result = Generate(input);

        result.Assignments.Should().HaveCount(2);
        var soloAssignment = result.Assignments.First(a => a.EnrollmentId.HasValue);
        soloAssignment.WeeklyTemplateEntryId.Should().Be(Slot2);
        result.Conflicts.Should().BeEmpty();
    }

    // ── Group assignment ─────────────────────────────────────────────────────

    [Test]
    public void Generate_GroupAssignedToSlotWithCapacity()
    {
        var group = MakeGroup("FamilyGroup", 3, new()
        {
            { Slot1, SlotPreference.Preferred },
            { Slot2, SlotPreference.Available },
        });

        var input = new SchedulingInput(
            [group],
            [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]);

        var result = Generate(input);

        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].WeeklyTemplateEntryId.Should().Be(Slot1);
        result.Assignments[0].GroupId.Should().NotBeNull();
        result.Assignments[0].Size.Should().Be(3);
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_GroupTooLargeForPreferredSlot_FallsBackToAvailable()
    {
        var group = MakeGroup("BigGroup", 3, new()
        {
            { Slot1, SlotPreference.Preferred },
            { Slot2, SlotPreference.Available },
        });

        var input = new SchedulingInput(
            [group],
            [new SlotInfo(Slot1, 2), new SlotInfo(Slot2, 4)]); // Slot1 too small

        var result = Generate(input);

        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].WeeklyTemplateEntryId.Should().Be(Slot2);
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_GroupTooLargeForAllSlots_Conflict()
    {
        var group = MakeGroup("HugeGroup", 5, new()
        {
            { Slot1, SlotPreference.Preferred },
            { Slot2, SlotPreference.Available },
        });

        var input = new SchedulingInput(
            [group],
            [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]);

        var result = Generate(input);

        result.Assignments.Should().BeEmpty();
        result.Conflicts.Should().HaveCount(5); // one per member
    }

    // ── Uncontested slots ────────────────────────────────────────────────────

    [Test]
    public void Generate_UncontestedSlot_AssignedImmediately()
    {
        var alice = MakeSolo("Alice", new() { { Slot1, SlotPreference.Available } });
        var bob = MakeSolo("Bob", new() { { Slot2, SlotPreference.Available } });

        var input = new SchedulingInput(
            [alice, bob],
            [new SlotInfo(Slot1, 4), new SlotInfo(Slot2, 4)]);

        var result = Generate(input);

        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().BeEmpty();
    }

    // ── Auto-grouping open solos ─────────────────────────────────────────────

    [Test]
    public void Generate_OpenSolosWithSharedPreference_AutoGrouped()
    {
        var alice = MakeSolo("Alice", new() { { Slot1, SlotPreference.Preferred } }, isOpenToGrouping: true);
        var bob = MakeSolo("Bob", new() { { Slot1, SlotPreference.Preferred } }, isOpenToGrouping: true);
        var carol = MakeSolo("Carol", new() { { Slot1, SlotPreference.Preferred } }, isOpenToGrouping: true);

        var input = new SchedulingInput(
            [alice, bob, carol],
            [new SlotInfo(Slot1, 4)]);

        var result = Generate(input);

        result.Assignments.Should().HaveCount(3);
        result.Assignments.Should().AllSatisfy(a => a.WeeklyTemplateEntryId.Should().Be(Slot1));
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_ClosedSolosNotAutoGrouped()
    {
        var alice = MakeSolo("Alice", new() { { Slot1, SlotPreference.Preferred } }, isOpenToGrouping: false);
        var bob = MakeSolo("Bob", new() { { Slot1, SlotPreference.Preferred } }, isOpenToGrouping: false);

        var input = new SchedulingInput(
            [alice, bob],
            [new SlotInfo(Slot1, 4)]);

        var result = Generate(input);

        // Both should still be assigned (capacity allows), just not via auto-grouping step
        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().BeEmpty();
    }

    // ── Oversubscribed / capacity ────────────────────────────────────────────

    [Test]
    public void Generate_OversubscribedSlot_ExcessBecomesConflict()
    {
        var solos = Enumerable.Range(1, 6).Select(i =>
            MakeSolo($"Student{i}", new() { { Slot1, SlotPreference.Preferred } })
        ).ToList();

        var input = new SchedulingInput(
            solos,
            [new SlotInfo(Slot1, 4)]);

        var result = Generate(input);

        result.Assignments.Should().HaveCount(4);
        result.Conflicts.Should().HaveCount(2);
    }

    [Test]
    public void Generate_MixedGroupAndSolos_GroupGetsPreference()
    {
        var group = MakeGroup("Family", 3, new() { { Slot1, SlotPreference.Preferred } });
        var solo = MakeSolo("Alice", new() { { Slot1, SlotPreference.Preferred } });

        var input = new SchedulingInput(
            [group, solo],
            [new SlotInfo(Slot1, 4)]); // capacity 4: group(3) + solo(1) = 4, fits

        var result = Generate(input);

        result.Assignments.Should().HaveCount(2);
        result.Conflicts.Should().BeEmpty();
    }

    [Test]
    public void Generate_MixedGroupAndSolos_NotEnoughCapacity()
    {
        var group = MakeGroup("Family", 3, new() { { Slot1, SlotPreference.Preferred } });
        var solo1 = MakeSolo("Alice", new() { { Slot1, SlotPreference.Preferred } });
        var solo2 = MakeSolo("Bob", new() { { Slot1, SlotPreference.Preferred } });

        var input = new SchedulingInput(
            [group, solo1, solo2],
            [new SlotInfo(Slot1, 4)]); // capacity 4: group(3) + 1 solo fits, 1 conflict

        var result = Generate(input);

        result.Assignments.Should().HaveCount(2); // group + 1 solo
        result.Conflicts.Should().HaveCount(1);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EnrollmentUnit MakeSolo(
        string name,
        Dictionary<Guid, SlotPreference> prefs,
        bool isOpenToGrouping = false)
    {
        var id = Guid.NewGuid();
        return new EnrollmentUnit(
            Id: id,
            IsGroup: false,
            GroupId: null,
            EnrollmentIds: [id],
            StudentNames: [name],
            Size: 1,
            IsOpenToGrouping: isOpenToGrouping,
            Preferences: prefs);
    }

    private static EnrollmentUnit MakeGroup(
        string name,
        int size,
        Dictionary<Guid, SlotPreference> prefs)
    {
        var groupId = Guid.NewGuid();
        var enrollmentIds = Enumerable.Range(0, size).Select(_ => Guid.NewGuid()).ToList();
        var names = Enumerable.Range(0, size).Select(i => $"{name}_{i}").ToList();
        return new EnrollmentUnit(
            Id: groupId,
            IsGroup: true,
            GroupId: groupId,
            EnrollmentIds: enrollmentIds,
            StudentNames: names,
            Size: size,
            IsOpenToGrouping: false,
            Preferences: prefs);
    }
}
