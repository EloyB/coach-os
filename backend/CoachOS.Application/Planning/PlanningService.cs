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
    ILessonRepository lessonRepo,
    ILogger<PlanningService> logger) : IPlanningService
{
    public async Task<Result<PlanningOverviewDto>> GenerateProposalAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

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

        // Build enrollment units
        var units = new List<EnrollmentUnit>();
        var groupedEnrollmentIds = new HashSet<Guid>();

        foreach (var group in groups)
        {
            var memberEnrollments = activeEnrollments
                .Where(e => e.EnrollmentGroupId == group.Id)
                .ToList();

            if (memberEnrollments.Count == 0) continue;

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

            units.Add(new EnrollmentUnit(
                Id: group.Id,
                IsGroup: true,
                GroupId: group.Id,
                EnrollmentIds: memberEnrollments.Select(e => e.Id).ToList(),
                StudentNames: memberEnrollments.Select(e => e.StudentName).ToList(),
                Size: memberEnrollments.Count,
                IsOpenToGrouping: false,
                Preferences: groupPrefs));

            foreach (var e in memberEnrollments)
                groupedEnrollmentIds.Add(e.Id);
        }

        foreach (var enrollment in activeEnrollments.Where(e => !groupedEnrollmentIds.Contains(e.Id)))
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

        var slotInfos = slots.Select(s => new SlotInfo(s.Id, s.MaxStudents)).ToList();
        var input = new SchedulingInput(units, slotInfos);

        // Run algorithm
        var result = SchedulingAlgorithm.Generate(input);

        // Clear existing proposed assignments
        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var proposedOnly = existingAssignments.Where(a => a.Status == ScheduleAssignmentStatus.Proposed).ToList();
        if (proposedOnly.Count > 0)
            scheduleAssignmentRepo.RemoveRange(proposedOnly);

        // Persist new assignments
        var newAssignments = result.Assignments.Select(a => new ScheduleAssignment
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = a.WeeklyTemplateEntryId,
            EnrollmentGroupId = a.GroupId,
            EnrollmentId = a.EnrollmentId,
            Status = ScheduleAssignmentStatus.Proposed,
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
        var activeEnrollments = enrollments
            .Where(e => e.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending)
            .ToList();
        var slots = series.WeeklyTemplate.ToList();

        // Build assigned enrollment IDs
        var assignedEnrollmentIds = new HashSet<Guid>();
        foreach (var assignment in assignments)
        {
            if (assignment.EnrollmentId.HasValue)
                assignedEnrollmentIds.Add(assignment.EnrollmentId.Value);
            if (assignment.EnrollmentGroup is not null)
                foreach (var member in assignment.EnrollmentGroup.Members)
                    assignedEnrollmentIds.Add(member.Id);
        }

        var slotDtos = slots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(slot =>
            {
                var slotAssignments = assignments
                    .Where(a => a.WeeklyTemplateEntryId == slot.Id)
                    .ToList();

                var currentCount = slotAssignments.Sum(a =>
                    a.EnrollmentGroup?.Members.Count ?? 1);

                return new SlotAssignmentDto
                {
                    WeeklyTemplateEntryId = slot.Id,
                    DayOfWeek = slot.DayOfWeek,
                    StartTime = slot.StartTime.ToString("HH:mm"),
                    EndTime = slot.EndTime.ToString("HH:mm"),
                    CourtName = slot.CourtName,
                    TrainerId = slot.TrainerId,
                    MaxCapacity = slot.MaxStudents,
                    CurrentCount = currentCount,
                    Assignments = slotAssignments.Select(a => new AssignedItemDto
                    {
                        AssignmentId = a.Id,
                        EnrollmentId = a.EnrollmentId,
                        GroupId = a.EnrollmentGroupId,
                        StudentNames = a.EnrollmentGroup is not null
                            ? a.EnrollmentGroup.Members.Select(m => m.StudentName).ToList()
                            : a.Enrollment is not null
                                ? [a.Enrollment.StudentName]
                                : [],
                        Status = a.Status.ToString(),
                    }).ToList(),
                };
            })
            .ToList();

        var unassigned = activeEnrollments
            .Where(e => !assignedEnrollmentIds.Contains(e.Id))
            .Select(e => new UnassignedEnrollmentDto
            {
                EnrollmentId = e.Id,
                StudentName = e.StudentName,
                Reason = "Niet toegewezen",
            })
            .ToList();

        return Result<PlanningOverviewDto>.Ok(new PlanningOverviewDto
        {
            PlanningStatus = series.PlanningStatus.ToString(),
            Slots = slotDtos,
            Unassigned = unassigned,
        });
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

        assignment.WeeklyTemplateEntryId = request.WeeklyTemplateEntryId;
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

        var slots = series.WeeklyTemplate.ToDictionary(w => w.Id);

        // Generate lessons for each unique slot that has assignments
        var assignedSlotIds = proposedAssignments.Select(a => a.WeeklyTemplateEntryId).Distinct();

        foreach (var slotId in assignedSlotIds)
        {
            if (!slots.TryGetValue(slotId, out var slot)) continue;

            var dates = GetWeeklyDates(series.StartDate, series.EndDate, (DayOfWeek)slot.DayOfWeek);

            foreach (var date in dates)
            {
                Lesson lesson = new()
                {
                    OrganizationId = organizationId,
                    LessonSerieId = seriesId,
                    Date = date,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    CourtName = slot.CourtName,
                    TrainerId = slot.TrainerId,
                    MaxStudents = slot.MaxStudents,
                    Level = series.Level,
                    IsCancelled = false,
                };

                await lessonRepo.AddAsync(lesson, ct);
            }
        }

        // Confirm all assignments
        foreach (var assignment in proposedAssignments)
            assignment.Status = ScheduleAssignmentStatus.Confirmed;

        series.PlanningStatus = PlanningStatus.Scheduled;
        await lessonSeriesRepo.SaveChangesAsync(ct);

        logger.LogInformation(
            "Planning bevestigd voor reeks {SeriesId}: {Count} toewijzingen, lessen gegenereerd",
            seriesId, proposedAssignments.Count);

        return Result<bool>.Ok(true);
    }

    private static IEnumerable<DateOnly> GetWeeklyDates(DateOnly start, DateOnly end, DayOfWeek day)
    {
        var current = start;

        // Advance to first occurrence of the target day
        var daysUntilTarget = ((int)day - (int)current.DayOfWeek + 7) % 7;
        current = current.AddDays(daysUntilTarget);

        while (current <= end)
        {
            yield return current;
            current = current.AddDays(7);
        }
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
