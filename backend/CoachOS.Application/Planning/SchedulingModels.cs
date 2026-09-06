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
    bool IsOpenToGrouping,
    Dictionary<Guid, SlotPreference> Preferences,
    string? AgeCategory = null);

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
    int Size,
    bool IsAutoMerged = false);

public record ConflictItem(
    Guid EnrollmentId,
    string StudentName,
    string Reason);
