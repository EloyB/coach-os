using CoachOS.Domain.Enums;

namespace CoachOS.Application.Planning;

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
    int MaxGroupSize,
    Dictionary<Guid, SlotPreference> Preferences)
{
    /// <summary>
    /// Het maximum dat de planner hanteert: nooit lager dan de eigen groep, ook niet bij onverwachte data.
    /// </summary>
    public int EffectiveMaxGroupSize => Math.Max(MaxGroupSize, Size);

    /// <summary>Wil (of aanvaardt) samen met anderen in een slot te zitten: het maximum is groter dan de eigen groep.</summary>
    public bool IsOpenToGrouping => EffectiveMaxGroupSize > Size;
}

/// <param name="WeeklyTemplateEntryId">Het weekslot.</param>
/// <param name="MaxCapacity">Nog vrije plaatsen (capaciteit min wie er al vastligt).</param>
/// <param name="OccupantCount">Aantal personen dat er al vastligt (vergrendelde of bevestigde toewijzingen).</param>
/// <param name="OccupantsMaxGroupSize">Kleinste maximum van wie er al vastligt: het slot mag niet groter worden dan dat.</param>
public record SlotInfo(
    Guid WeeklyTemplateEntryId,
    int MaxCapacity,
    int OccupantCount = 0,
    int OccupantsMaxGroupSize = int.MaxValue);

public record SchedulingResult(
    List<ProposedAssignment> Assignments,
    List<ConflictItem> Conflicts);

public record ProposedAssignment(
    Guid WeeklyTemplateEntryId,
    Guid? GroupId,
    Guid? EnrollmentId,
    int Size,
    bool IsAutoMerged = false);

public record ConflictItem(
    Guid EnrollmentId,
    string StudentName,
    string Reason);
