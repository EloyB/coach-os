using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Planning;

public class PlanningService(
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    IUserLookupService userLookup,
    ILogger<PlanningService> logger) : IPlanningService
{
    public async Task<Result<PlanningOverviewDto>> GenerateProposalAsync(
        Guid seriesId, Guid organizationId, bool force = false, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Protect manual edits: after the first generate the status flips to Planning.
        // Re-running then would wipe admin tweaks — require an explicit force flag.
        if (series.PlanningStatus == PlanningStatus.Planning && !force)
            return await GetPlanningOverviewAsync(seriesId, organizationId, ct);

        // Load all data
        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var activeEnrollments = enrollments
            .Where(e => e.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending)
            .ToList();
        var groups = await enrollmentGroupRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var preferences = await timeSlotPreferenceRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var slots = series.WeeklyTemplate.ToList();

        if (slots.Count == 0)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.Validation, "Er zijn nog geen tijdslots aangemaakt."));

        // Build preference lookup: enrollmentId -> { slotId -> preference }
        var prefsByEnrollment = preferences
            .GroupBy(p => p.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(p => p.WeeklyTemplateEntryId, p => p.Preference));

        // On force-regenerate, preserve manually locked assignments: exclude their
        // units from the algorithm and reduce the affected slots' remaining capacity.
        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var lockedAssignments = existingAssignments
            .Where(a => a.IsLocked && a.Status == ScheduleAssignmentStatus.Proposed)
            .ToList();

        var lockedGroupIds = lockedAssignments
            .Where(a => a.EnrollmentGroupId.HasValue)
            .Select(a => a.EnrollmentGroupId!.Value)
            .ToHashSet();
        var lockedEnrollmentIds = lockedAssignments
            .Where(a => a.EnrollmentId.HasValue)
            .Select(a => a.EnrollmentId!.Value)
            .ToHashSet();

        var capacityUsedBySlot = lockedAssignments
            .GroupBy(a => a.WeeklyTemplateEntryId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.EnrollmentGroup?.Members.Count ?? 1));

        // Build enrollment units
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

            // Use leader's preferences for the group (members don't submit individual preferences).
            // Fall back to intersection if multiple members have preferences.
            var membersWithPrefs = memberEnrollments
                .Where(e => prefsByEnrollment.ContainsKey(e.Id))
                .ToList();

            Dictionary<Guid, SlotPreference> groupPrefs;
            if (membersWithPrefs.Count == 0)
                groupPrefs = new();
            else if (membersWithPrefs.Count == 1)
                groupPrefs = prefsByEnrollment[membersWithPrefs[0].Id];
            else
                groupPrefs = IntersectPreferences(
                    membersWithPrefs.Select(e => prefsByEnrollment[e.Id]).ToList());

            var leaderEnrollment = memberEnrollments.FirstOrDefault(e => e.Id == group.LeaderEnrollmentId);
            units.Add(new EnrollmentUnit(
                Id: group.Id,
                IsGroup: true,
                GroupId: group.Id,
                EnrollmentIds: memberEnrollments.Select(e => e.Id).ToList(),
                StudentNames: memberEnrollments.Select(e => e.StudentName).ToList(),
                Size: memberEnrollments.Count,
                IsOpenToGrouping: leaderEnrollment?.IsOpenToGrouping ?? false,
                Preferences: groupPrefs));

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
                Preferences: prefs));
        }

        var slotInfos = slots
            .Select(s => new SlotInfo(
                s.Id,
                Math.Max(0, s.MaxStudents - capacityUsedBySlot.GetValueOrDefault(s.Id, 0))))
            .ToList();
        var input = new SchedulingInput(units, slotInfos);

        // Run algorithm
        var result = SchedulingAlgorithm.Generate(input);

        // Clear existing proposed assignments (uses ExecuteDelete to avoid tracking conflicts)
        await scheduleAssignmentRepo.RemoveProposedBySeriesAsync(seriesId, organizationId, ct);

        // Persist new assignments
        var newAssignments = result.Assignments.Select(a => new ScheduleAssignment
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = a.WeeklyTemplateEntryId,
            EnrollmentGroupId = a.GroupId,
            EnrollmentId = a.EnrollmentId,
            Status = ScheduleAssignmentStatus.Proposed,
            IsAutoMerged = a.IsAutoMerged,
        });

        await scheduleAssignmentRepo.AddRangeAsync(newAssignments, ct);

        // Update planning status
        series.PlanningStatus = PlanningStatus.Planning;
        await lessonSeriesRepo.SaveChangesAsync(ct);

        return await GetPlanningOverviewAsync(seriesId, organizationId, ct);
    }

    public async Task<Result<PlanningOverviewDto>> GetPlanningOverviewAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var groups = await enrollmentGroupRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var preferences = await timeSlotPreferenceRepo.GetBySeriesAsync(seriesId, organizationId, ct);

        var activeEnrollments = enrollments
            .Where(e => e.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending)
            .ToList();

        var prefsRawByEnrollment = preferences
            .GroupBy(p => p.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(p => p.WeeklyTemplateEntryId, p => p.Preference));
        var prefsByEnrollment = prefsRawByEnrollment
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(p => p.Key, p => p.Value.ToString()));

        var trainerIds = series.WeeklyTemplate
            .Where(s => s.TrainerId.HasValue)
            .Select(s => s.TrainerId!.Value)
            .Distinct()
            .ToList();
        var trainerNames = trainerIds.Count > 0
            ? await userLookup.GetUserNamesByIdsAsync(trainerIds, ct)
            : new Dictionary<Guid, string>();

        var timeSlotDtos = series.WeeklyTemplate
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(slot => new PlanningTimeSlotDto
            {
                Id = slot.Id,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime.ToString("HH:mm"),
                EndTime = slot.EndTime.ToString("HH:mm"),
                CourtName = slot.CourtName,
                TrainerId = slot.TrainerId,
                TrainerName = slot.TrainerId.HasValue
                    ? trainerNames.GetValueOrDefault(slot.TrainerId.Value)
                    : null,
                MaxCapacity = slot.MaxStudents,
            })
            .ToList();

        var enrollmentDtos = activeEnrollments
            .Select(e => new PlanningEnrollmentDto
            {
                Id = e.Id,
                StudentName = e.StudentName,
                StudentEmail = e.StudentEmail,
                StudentPhone = e.StudentPhone,
                IsOpenToGrouping = e.IsOpenToGrouping,
                GroupId = e.EnrollmentGroupId,
                Preferences = prefsByEnrollment.GetValueOrDefault(e.Id, new()),
            })
            .ToList();

        var groupDtos = groups
            .Select(g => new PlanningGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                LeaderEnrollmentId = g.LeaderEnrollmentId,
                MemberEnrollmentIds = activeEnrollments
                    .Where(e => e.EnrollmentGroupId == g.Id)
                    .Select(e => e.Id)
                    .ToList(),
            })
            .ToList();

        var assignmentDtos = assignments
            .Select(a => new PlanningAssignmentDto
            {
                Id = a.Id,
                TimeSlotId = a.WeeklyTemplateEntryId,
                EnrollmentId = a.EnrollmentId,
                GroupId = a.EnrollmentGroupId,
                Status = a.Status.ToString(),
                IsAutoMerged = a.IsAutoMerged,
                IsLocked = a.IsLocked,
            })
            .ToList();

        DateTime? lastEditedAt = assignments.Count > 0
            ? assignments.Max(a => a.UpdatedAt == default ? a.CreatedAt : a.UpdatedAt)
            : null;

        var conflicts = BuildConflicts(
            activeEnrollments, assignments, series.WeeklyTemplate.ToList(), prefsRawByEnrollment);

        return Result<PlanningOverviewDto>.Ok(new PlanningOverviewDto
        {
            PlanningStatus = series.PlanningStatus.ToString(),
            PlanningLastEditedAt = lastEditedAt,
            TimeSlots = timeSlotDtos,
            Enrollments = enrollmentDtos,
            Groups = groupDtos,
            Assignments = assignmentDtos,
            Conflicts = conflicts,
        });
    }

    private static List<PlanningConflictDto> BuildConflicts(
        List<Enrollment> activeEnrollments,
        List<ScheduleAssignment> assignments,
        List<WeeklyTemplateEntry> slots,
        Dictionary<Guid, Dictionary<Guid, SlotPreference>> prefsByEnrollment)
    {
        var conflicts = new List<PlanningConflictDto>();

        // Assigned enrollment IDs (solo + group members)
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

    public async Task<Result<bool>> DeleteAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Bevestigde toewijzingen kunnen niet verwijderd worden."));

        scheduleAssignmentRepo.RemoveRange([assignment]);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateAssignmentAsync(
        Guid seriesId, Guid assignmentId, UpdateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Bevestigde toewijzingen kunnen niet verplaatst worden."));

        // Capacity guard on target slot (excluding the assignment being moved)
        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId,
            addSize: assignment.EnrollmentGroup?.Members.Count ?? 1,
            excludeAssignmentId: assignment.Id, ct);
        if (capacityError is not null)
            return Result<bool>.Fail(capacityError);

        assignment.WeeklyTemplateEntryId = request.WeeklyTemplateEntryId;
        assignment.IsLocked = true;
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CreateAssignmentAsync(
        Guid seriesId, CreateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Verify enrollment belongs to this series
        var enrollment = await enrollmentRepo.GetByIdAsync(request.EnrollmentId, organizationId, ct);
        if (enrollment is null || enrollment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        // Verify slot belongs to this series
        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == request.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        // Check if enrollment already has an assignment
        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        bool alreadyAssigned = existingAssignments.Any(a => a.EnrollmentId == request.EnrollmentId);
        if (alreadyAssigned)
            return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Inschrijving is al toegewezen."));

        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId, addSize: 1, excludeAssignmentId: null, ct);
        if (capacityError is not null)
            return Result<bool>.Fail(capacityError);

        ScheduleAssignment assignment = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = request.WeeklyTemplateEntryId,
            EnrollmentId = request.EnrollmentId,
            Status = ScheduleAssignmentStatus.Proposed,
            IsAutoMerged = false,
            IsLocked = true,
        };

        await scheduleAssignmentRepo.AddRangeAsync([assignment], ct);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<Guid>> CreateGroupAsync(
        Guid seriesId, CreateGroupRequest request, Guid organizationId, CancellationToken ct = default)
    {
        var exists = await lessonSeriesRepo.ExistsAsync(seriesId, organizationId, ct);
        if (!exists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var selected = enrollments.Where(e => request.EnrollmentIds.Contains(e.Id)).ToList();

        if (selected.Count != request.EnrollmentIds.Count)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen niet gevonden."));

        if (selected.Any(e => e.EnrollmentGroupId.HasValue))
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen zitten al in een groep."));

        var existingGroupCount = await enrollmentGroupRepo.CountBySeriesAsync(seriesId, organizationId, ct);
        var groupLetter = (char)('A' + existingGroupCount);

        EnrollmentGroup group = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            Name = $"Groep {groupLetter}",
            LeaderEnrollmentId = selected[0].Id,
        };

        await enrollmentGroupRepo.AddAsync(group, ct);

        foreach (var enrollment in selected)
            enrollment.EnrollmentGroupId = group.Id;

        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(group.Id);
    }

    public async Task<Result<bool>> DissolveGroupAsync(
        Guid seriesId, Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        var group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        // Remove group reference from members
        foreach (var member in group.Members)
            member.EnrollmentGroupId = null;

        // Remove any assignments that reference this group
        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var groupAssignments = assignments.Where(a => a.EnrollmentGroupId == groupId).ToList();
        if (groupAssignments.Count > 0)
            scheduleAssignmentRepo.RemoveRange(groupAssignments);

        enrollmentGroupRepo.Delete(group);
        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ConfirmScheduleAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        if (series.PlanningStatus != PlanningStatus.Planning)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Planning moet eerst gegenereerd worden."));

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var proposedAssignments = assignments.Where(a => a.Status == ScheduleAssignmentStatus.Proposed).ToList();

        if (proposedAssignments.Count == 0)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Er zijn geen voorgestelde toewijzingen om te bevestigen."));

        // Confirm all assignments
        foreach (var assignment in proposedAssignments)
            assignment.Status = ScheduleAssignmentStatus.Confirmed;

        series.PlanningStatus = PlanningStatus.Scheduled;
        await lessonSeriesRepo.SaveChangesAsync(ct);

        logger.LogInformation(
            "Planning bevestigd voor reeks {SeriesId}: {Count} toewijzingen",
            seriesId, proposedAssignments.Count);

        return Result<bool>.Ok(true);
    }

    private async Task<Error?> EnsureSlotCapacityAsync(
        Guid seriesId, Guid organizationId, Guid slotId, int addSize,
        Guid? excludeAssignmentId, CancellationToken ct)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        var slot = series?.WeeklyTemplate.FirstOrDefault(s => s.Id == slotId);
        if (slot is null) return null; // caller already validated

        var existing = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var currentCount = existing
            .Where(a => a.WeeklyTemplateEntryId == slotId && a.Id != excludeAssignmentId)
            .Sum(a => a.EnrollmentGroup?.Members.Count ?? 1);

        if (currentCount + addSize > slot.MaxStudents)
            return new Error(ErrorCodes.Validation,
                $"Tijdslot heeft geen plaats meer ({currentCount}/{slot.MaxStudents}).");

        return null;
    }

    private static Dictionary<Guid, SlotPreference> IntersectPreferences(
        List<Dictionary<Guid, SlotPreference>> memberPrefs)
    {
        if (memberPrefs.Count == 0) return new();
        if (memberPrefs.Count == 1) return memberPrefs[0];

        var allSlotIds = memberPrefs
            .SelectMany(p => p.Keys)
            .Distinct();

        var result = new Dictionary<Guid, SlotPreference>();

        foreach (var slotId in allSlotIds)
        {
            // Take worst preference among members (most restrictive)
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

            if (!allHave)
                result[slotId] = SlotPreference.Unavailable;
            else
                result[slotId] = worstPref;
        }

        return result;
    }
}
